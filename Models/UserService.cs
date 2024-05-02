using System.Security.Cryptography;
using System.Text;

namespace WebApplication2.Models
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;            
        }

        public User Authenticate(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email.ToLower() == email.ToLower());

            if (user == null || !VerifyPasswordHash(password, user.Password))
                return null;

            return user;
        }

        public User Register(string firstName, string lastName, string patronymic, string phone, string email, string password, string role)
        {
            if (_context.Users.Any(x => x.Email == email))
                return null;

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Patronymic = patronymic,
                Phone = phone,
                Email = email,
                Role = role
            };

            user.Password = HashPassword(password);

            _context.Users.Add(user);
            _context.SaveChanges(); // сохраняем изменения в базе данных

            return user;
        }


        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private static bool VerifyPasswordHash(string password, string hashedPassword)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPasswordToCheck = Convert.ToBase64String(hashedBytes);
                return hashedPasswordToCheck == hashedPassword;
            }
        }
    }
}
