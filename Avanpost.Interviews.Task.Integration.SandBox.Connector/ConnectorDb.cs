using Avanpost.Interviews.Task.Integration.Data.DbCommon;
using Avanpost.Interviews.Task.Integration.Data.DbCommon.DbModels;
using Avanpost.Interviews.Task.Integration.Data.Models;
using Avanpost.Interviews.Task.Integration.Data.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using System.Data.Common;

namespace Avanpost.Interviews.Task.Integration.SandBox.Connector
{
    public class ConnectorDb : IConnector
    {
        private DataContext context;
        public void StartUp(string connectionString)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
                var connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
                if (connectionStringBuilder["Provider"].ToString()!.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                    optionsBuilder.UseNpgsql(connectionStringBuilder["ConnectionString"].ToString());
                else if (connectionStringBuilder["Provider"].ToString()!.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                    optionsBuilder.UseSqlServer(connectionStringBuilder["ConnectionString"].ToString());
                else throw new Exception("Неопределенный провайдер - " + connectionStringBuilder["Provider"]);
                context = new DataContext(optionsBuilder.Options);
            }
            catch(Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        public void CreateUser(UserToCreate user)
        {
            try
            {
                if (IsUserExists(user.Login)) throw new Exception($"Пользователь {user.Login} уже существует.");
                var properties = user.Properties.ToDictionary(x => x.Name, x => x.Value);   
                var userToAdd = new User()
                {
                    Login = user.Login,
                    FirstName = properties.GetValueOrDefault("firstName", ""),
                    MiddleName = properties.GetValueOrDefault("middleName", ""),
                    LastName = properties.GetValueOrDefault("lastName", ""),
                    TelephoneNumber = properties.GetValueOrDefault("telephoneNumber", ""),
                    IsLead = bool.Parse(properties.GetValueOrDefault("isLead")!)
                };

                var sequrity = new Sequrity()
                {
                    UserId = userToAdd.Login,
                    Password = user.HashPassword
                };

                context.Users.Add(userToAdd);
                context.Passwords.Add(sequrity);
                context.SaveChanges();
            }
            catch(Exception e) {

                Logger.Error(e.Message);
                throw;
            }
        }
        //Если честно, я не совсем понял, что от меня ожидалось сделать, но поскольку в тестовом задании сказано, что пароль тоже свойство, то только его я и добавлю.
        //Но с паролем оно не может пройти тест, так как в тесте указано 5 свойств, но здесь у него 6: lastname, firstName, middleNMame, telephoneNumber, isLead, password
        public IEnumerable<Property> GetAllProperties()
        {
            var allproperties = context!
                    .Model
                    .FindEntityType(typeof(User))?
                    .GetProperties()
                    .Where(x => !x.IsKey())
                    .Select(x => new Property(x.Name, String.Empty))
                    .ToList();
            allproperties.Add(new Property("password", String.Empty));
            foreach (var p in allproperties)
            {
                Logger.Error(p.Name);
            }
            return allproperties;
        }

        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            try
            {
                User user = context.Users.Find(userLogin);
                if (user == null) throw new Exception("Пользователь не существует");
                return context.Entry(user)
                    .Properties.Where(p => !p.Metadata.IsKey())
                    .Select(p => new UserProperty(p.Metadata.Name, $"{p.CurrentValue}"));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

        }

        public bool IsUserExists(string userLogin)
        {
            return context.Users.Any(e => e.Login == userLogin);
        }

        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            try
            {
                User user = context.Users.Find(userLogin);
                if (user == null) throw new Exception("Пользователь не существует");
                foreach (UserProperty p in properties)
                {
                    if (p.Name == "password")
                    {
                        Sequrity sequrity = context.Passwords.Single(e => e.UserId == userLogin);
                        sequrity.Password = p.Value;

                    }
                    context.Entry(user).Property(p.Name).CurrentValue = p.Value;
                }
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        public IEnumerable<Permission> GetAllPermissions()
        {
            var rights = context.RequestRights.Select(p => new Permission(p.Id.ToString()!, p.Name, "")).ToList();
            var roles = context.ITRoles.Select(r => new Permission(r.Id.ToString()!, r.Name, "")).ToList();
            return rights.Concat(roles);
        }

        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                User user = context.Users.Find(userLogin);
                if (user == null) throw new Exception("Пользователь не существует");
                foreach (string p in rightIds)
                {
                    string permissionType = p.Split(':')[0];
                    int permissionId = Convert.ToInt32(p.Split(':')[1]);
                    if (permissionType == "Role")
                        context.UserITRoles.Add(new UserITRole { UserId = userLogin, RoleId = permissionId });
                    else if (permissionType == "Request")
                        context.UserRequestRights.Add(new UserRequestRight { UserId = userLogin, RightId = permissionId });
                }
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }

        }

        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                User user = context.Users.Find(userLogin);
                if (user == null) throw new Exception("Пользователь не существует");
                foreach (string p in rightIds)
                {
                    string permissionType = p.Split(':')[0];
                    int roleId = Convert.ToInt32(p.Split(':')[1]);
                    if (permissionType == "Role")
                    {
                        var userITRole = context.UserITRoles.First(x => x.UserId == userLogin && x.RoleId == roleId);
                        context.UserITRoles.Remove(userITRole);
                    }
                    else if (permissionType == "Request")
                    {
                        UserRequestRight userRequestRight = context.UserRequestRights.First(x => x.UserId == userLogin && x.RightId == roleId);
                        context.UserRequestRights.Remove(userRequestRight);
                    }
                }
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        public IEnumerable<string> GetUserPermissions(string userLogin)
        {
            try
            {
                User user = context.Users.Find(userLogin);
                if (user == null) throw new Exception("Пользователь не существует");
                var userRoles = context
                    .UserRequestRights.Where(e => e.UserId == userLogin)
                    .Select(e => $"Request:{e.RightId}")
                    .ToList();
                var userRights = context
                    .UserITRoles.Where(e => e.UserId == userLogin)
                    .Select(e => $"Role:{e.RoleId}")
                    .ToList();

                return userRoles.Concat(userRights);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        public ILogger Logger { get; set; }
    }
}