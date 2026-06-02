using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using System.Threading.Tasks;

namespace ServiceDeskDesktop.Services
{
    public class NotificationService
    {
        private readonly FirestoreDb db = FirebaseService.Instance.FirestoreDb;

        public async Task SendPushAsync(string engineerId, string requestId, string priority)
        {
            var userDoc = await db.Collection("users").Document(engineerId).GetSnapshotAsync();
            if (!userDoc.Exists) return;

            var token = userDoc.GetValue<string>("fcmToken");
            if (string.IsNullOrEmpty(token)) return;

            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = $"Новая заявка: {requestId}",
                    Body = $"Приоритет: {priority}. Проверьте список заявок."
                }
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
    }
}