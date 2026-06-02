using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace ServiceDeskDesktop.Models
{
    [FirestoreData]
    public class ServiceRequest
    {
        [FirestoreDocumentId]
        public string DocumentId { get; set; }

        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string ClientInfo { get; set; }

        [FirestoreProperty]
        public string Address { get; set; }

        [FirestoreProperty]
        public string Phone { get; set; }

        [FirestoreProperty]
        public string EquipmentType { get; set; }

        [FirestoreProperty]
        public string IssueDescription { get; set; }

        [FirestoreProperty]
        public string Status { get; set; }

        [FirestoreProperty]
        public string Priority { get; set; }

        [FirestoreProperty]
        public string SerialNumber { get; set; }

        [FirestoreProperty]
        public string AssignedEngineerId { get; set; }

        [FirestoreProperty]
        public string AssignedEngineerEmail { get; set; }

        [FirestoreProperty]
        public string InternalComment { get; set; }

        [FirestoreProperty]
        public string ExternalComment { get; set; }

        [FirestoreProperty]
        public Dictionary<string, bool> DispatcherFlags { get; set; } = new()
        {
            { "requiresClientApproval", false },
            { "awaitingParts", false }
        };

        [FirestoreProperty]
        public List<HistoryEntry> History { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [FirestoreProperty(Name = "createdAt")]
        private Google.Cloud.Firestore.Timestamp CreatedAtFirestore
        {
            get => Google.Cloud.Firestore.Timestamp.FromDateTime(CreatedAt.ToUniversalTime());
            set => CreatedAt = value.ToDateTime().ToLocalTime();
        }

        public DateTime LastModified { get; set; } = DateTime.Now;

        [FirestoreProperty(Name = "lastModified")]
        private Google.Cloud.Firestore.Timestamp LastModifiedFirestore
        {
            get => Google.Cloud.Firestore.Timestamp.FromDateTime(LastModified.ToUniversalTime());
            set => LastModified = value.ToDateTime().ToLocalTime();
        }

        public DateTime? WorkStartedAt { get; set; }

        [FirestoreProperty(Name = "workStartedAt")]
        private Google.Cloud.Firestore.Timestamp? WorkStartedAtFirestore
        {
            get => WorkStartedAt.HasValue ? Google.Cloud.Firestore.Timestamp.FromDateTime(WorkStartedAt.Value.ToUniversalTime()) : null;
            set => WorkStartedAt = value?.ToDateTime().ToLocalTime();
        }

        public DateTime? WorkCompletedAt { get; set; }

        [FirestoreProperty(Name = "workCompletedAt")]
        private Google.Cloud.Firestore.Timestamp? WorkCompletedAtFirestore
        {
            get => WorkCompletedAt.HasValue ? Google.Cloud.Firestore.Timestamp.FromDateTime(WorkCompletedAt.Value.ToUniversalTime()) : null;
            set => WorkCompletedAt = value?.ToDateTime().ToLocalTime();
        }

        public string CreatedAtFormatted
        {
            get
            {
                if (CreatedAt.Year < 2000) return "—";
                return CreatedAt.ToString("dd.MM.yyyy HH:mm");
            }
        }
    }

    [FirestoreData]
    public class HistoryEntry
    {
        [FirestoreProperty]
        public string Action { get; set; }

        [FirestoreProperty]
        public string UserId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [FirestoreProperty(Name = "timestamp")]
        private Google.Cloud.Firestore.Timestamp TimestampFirestore
        {
            get => Google.Cloud.Firestore.Timestamp.FromDateTime(Timestamp.ToUniversalTime());
            set => Timestamp = value.ToDateTime().ToLocalTime();
        }

        [FirestoreProperty]
        public string Details { get; set; }
    }
}