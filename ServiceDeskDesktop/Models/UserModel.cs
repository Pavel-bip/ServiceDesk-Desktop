using Google.Cloud.Firestore;
using System;

namespace ServiceDeskDesktop.Models
{
    [FirestoreData]
    public class UserModel
    {
        [FirestoreDocumentId]
        public string Uid { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string FullName { get; set; }

        [FirestoreProperty]
        public string Role { get; set; }

        [FirestoreProperty]
        public string Phone { get; set; }

        [FirestoreProperty]
        public bool IsBlocked { get; set; }

        public DateTime? LastSignIn { get; set; }

        public string LastSignInFormatted =>
            LastSignIn?.ToString("dd.MM.yyyy HH:mm") ?? "Никогда";
    }
}