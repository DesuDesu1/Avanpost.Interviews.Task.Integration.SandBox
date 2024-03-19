# Задание
* Развернуть бд (Postgres или MSSQL) в Docker или с помощью других средств;
* Заполнить тестовыми данными с помощью утилиты Avanpost.Integration.DbCreationUtility;
* Реализовать интерфейс коннектора:
```csharp
        public ILogger Logger { get; set; } //Логгер, нужен для логирования в тестах
        void StartUp(string connectionString); //Конфигурация коннектора через строку подключения (настройки для подключения к ресурсу(строка подключения к бд, 
        // путь к хосту с логином и паролем, дополнительные параметры конфигурации бизнес-логики и тд, формат любой, например: "key1=value1;key2=value2...";
        void CreateUser(UserToCreate user);// Создать пользователя со свойствами
        IEnumerable<Property> GetAllProperties();// Получить все свойства, которые можно получить для пользователя, пароль тоже свойство
        IEnumerable<UserProperty> GetUserProperties(string userLogin);// Получить все значения свойств пользователя
        bool IsUserExists(string userLogin);// Проверка существования пользователя
        void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin);// Изменения значений свойств пользователя
        IEnumerable<Permission> GetAllPermissions();// Получить все права в системе
        void AddUserPermissions(string userLogin, IEnumerable<string> rightIds);// Добавить права пользователю в системе
        void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds);// Удалить права пользователю в системе
        IEnumerable<string> GetUserPermissions(string userLogin);// Получить права пользователя в системе
```

# Описание системы
Система представляет собой сервис для технического обслуживания.
Внутри сервиса существуют пользователи User, выполняющие разные роли.
Каждый пользователь обладает свойствами определнными его моделью в БД.
Пользователи владеют правами двух типов RequestRight и ItRole.
Инициализация конфигурации через метод StartUp и обязательное требование -  наличие пустого конструктора.

# Таблицы БД:
* Таблица с пользователями User(Ненулевые стобцы login, isLead);
* Таблица с паролями Passwords(все столбцы ненулевые);
* Таблица с правами по изменению заявок RequestRight;
* Таблица с ролями исполнителей ItRole;
* Таблицы для связи пользователей и прав UserItRole, UserRequestRight(Все столбцы ненулевые, изменение прав пользователя состоит в добавлении и удалении данных из этих таблиц);

# Развертывание системы
Для создание схемы, таблиц и заполнения данными используется утилита Avanpost.Integration.DbCreationUtility(папка DbCreationUtility). Поддерживаются MSSQL и Postgre. Поддерживаемые значения параметра -p POSTGRE, MSSQL.

команды:
Avanpost.Integration.DbCreationUtility -s "строка подключения к бд" -p "провайдер бд"
пример: Avanpost.Integration.DbCreationUtility -s "Server=127.0.0.1;Port=5432;Database=testDb;Username=testUser;Password=12345678;" -p "POSTGRE"

# Структура решения:
* Avanpost.Interviews.Task.Integration.SandBox.Tests - проект с тестами коннектора;
* Avanpost.Interviews.Task.Integration.SandBox.Connector - проект с реализуемым коннектором

# Требования по реализации интерфейса коннектора
* Коннектор реализует интерфейс IConnector (все методы интерфейса);
* Коннектор проходит все тесты
* Коннектор не изменяет данные в таблицах RequestRights и ItRole;
* Коннектор использует логирование через свойство Logger;
* При работе с Permission разделяет ItRole и RequestRight;


