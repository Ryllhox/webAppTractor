namespace WebApplication2.Models
{
    public interface IUserService
    {
        User Authenticate(string email, string password);
        User Register(string firstName, string lastName, string patronymic, string phone, string email, string password, string role);
    }
}
