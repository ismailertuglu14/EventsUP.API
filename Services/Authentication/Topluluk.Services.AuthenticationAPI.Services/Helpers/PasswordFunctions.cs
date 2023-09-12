using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace Topluluk.Services.AuthenticationAPI.Services.Helpers
{
	public static class PasswordFunctions
	{
        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = GetHash(password, salt);
            return Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);
        }

        public static byte[] GetHash(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwordAndSalt = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, passwordAndSalt, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, passwordAndSalt, passwordBytes.Length, salt.Length);
                return sha256.ComputeHash(passwordAndSalt);
            }
        }



        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string[] parts = hashedPassword.Split('|');
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);
            byte[] actualHash = GetHash(password, salt);
            return StructuralComparisons.StructuralEqualityComparer.Equals(actualHash, expectedHash);
        }

        
        
    }
}

