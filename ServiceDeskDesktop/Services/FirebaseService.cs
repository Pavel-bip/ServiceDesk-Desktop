using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System;
using System.IO;

namespace ServiceDeskDesktop.Services
{
    public class FirebaseService
    {
        private static FirebaseService _instance;
        public FirestoreDb FirestoreDb { get; private set; }

        public static FirebaseService Instance => _instance ??= new FirebaseService();

        private FirebaseService()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "serviceAccountKey.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(path)
            });

            // ЗАМЕНИ "pavel-90348" на твой Project ID (тот, что был в JSON файле)
            FirestoreDb = FirestoreDb.Create("pavel-90348");
        }
    }
}