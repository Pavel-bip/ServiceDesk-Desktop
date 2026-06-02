using Google.Cloud.Firestore;
using Serilog;
using ServiceDeskDesktop.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceDeskDesktop.Services
{
    public class SyncService
    {
        public event Action<string, DateTime> OnConflictDetected;
        private readonly FirestoreDb db = FirebaseService.Instance.FirestoreDb;
        private readonly LocalDatabaseService localDb = new();

        public async Task SyncAllAsync()
        {
            try
            {
                // 1. Отправляем грязные записи
                var pendings = localDb.GetPendingOperations();
                foreach (var (request, operation) in pendings)
                {
                    try
                    {
                        if (operation == "delete")
                        {
                            await db.Collection("requests").Document(request.DocumentId).DeleteAsync();
                            localDb.DeleteRequest(request.DocumentId);
                        }
                        else if (operation == "create")
                        {
                            request.LastModified = DateTime.Now;
                            var docRef = await db.Collection("requests").AddAsync(request);
                            localDb.DeleteRequest(request.DocumentId);
                            request.DocumentId = docRef.Id;
                            localDb.SaveRequest(request, false);
                        }
                        else if (operation == "update")
                        {
                            request.LastModified = DateTime.Now;
                            await db.Collection("requests").Document(request.DocumentId).SetAsync(request, SetOptions.MergeAll);
                            localDb.SaveRequest(request, false);
                        }
                        Log.Information("Синхронизирована операция {Op} для заявки {Id}", operation, request.Id);
                    }
                    catch (Exception ex) { Log.Error(ex, "Ошибка синхронизации операции"); }
                }

                // 2. Загружаем свежие данные из облака
                var snapshot = await db.Collection("requests").GetSnapshotAsync();
                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var serverReq = doc.ConvertTo<ServiceRequest>();
                        serverReq.DocumentId = doc.Id;
                        var locals = localDb.GetAllRequests();
                        var existing = locals.FirstOrDefault(r => r.DocumentId == doc.Id);

                        if (existing != null)
                        {
                            // LWW: если серверная версия новее локальной
                            if (serverReq.LastModified > existing.LastModified)
                            {
                                localDb.SaveRequest(serverReq, false);
                                Log.Warning("Заявка {Id} обновлена с сервера (LWW). Локальная версия была старше.", serverReq.Id);

                                // Уведомление диспетчера
                                OnConflictDetected?.Invoke(serverReq.Id, serverReq.LastModified);
                            }
                        }
                        else
                        {
                            localDb.SaveRequest(serverReq, false);
                        }
                    }
                    catch { }
                }

                Log.Information("Синхронизация завершена успешно");
            }
            catch (Exception ex) { Log.Error(ex, "Ошибка синхронизации"); }
        }

        public async Task AddRequestAsync(ServiceRequest request)
        {
            request.LastModified = DateTime.Now;
            var docRef = await db.Collection("requests").AddAsync(request);
            request.DocumentId = docRef.Id;
            localDb.SaveRequest(request, false);
        }

        public async Task UpdateRequestAsync(ServiceRequest request)
        {
            request.LastModified = DateTime.Now;
            await db.Collection("requests").Document(request.DocumentId).SetAsync(request, SetOptions.MergeAll);
            localDb.SaveRequest(request, false);
        }

        public async Task DeleteRequestAsync(string documentId)
        {
            await db.Collection("requests").Document(documentId).DeleteAsync();
            localDb.DeleteRequest(documentId);
        }
    }
}