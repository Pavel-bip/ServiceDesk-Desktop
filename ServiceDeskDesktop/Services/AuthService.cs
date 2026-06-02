using FirebaseAdmin.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceDeskDesktop.Services
{
    public class AuthService
    {
        public async Task<string> CreateUserAsync(string email, string password, string fullName, string role)
        {
            var args = new UserRecordArgs()
            {
                Email = email,
                Password = password,
                DisplayName = fullName
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

            // Устанавливаем роль через custom claims
            var claims = new Dictionary<string, object>
            {
                { "role", role }
            };
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userRecord.Uid, claims);

            return userRecord.Uid;
        }

        public async Task DisableUserAsync(string uid)
        {
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = uid,
                Disabled = true
            });
        }

        public async Task EnableUserAsync(string uid)
        {
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = uid,
                Disabled = false
            });
        }

        public async Task ResetPasswordAsync(string uid, string newPassword)
        {
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = uid,
                Password = newPassword
            });
        }
        public async Task DeleteUserAsync(string uid)
        {
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
        }
    }
}