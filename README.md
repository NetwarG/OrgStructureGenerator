# Генератор оргструктуры

После развертывания очередной виртуальной машины и заполнения оргструктуры, было решено упростить этот момент, путем заполнения всех данных без ручного ввода. Так как для тестирования некоторых доработок, порой необходимо несколько тысяч записей, то было решено отказаться от заранее подготовленных SQL скриптов и написать собственную программу, генерирующую все необходимые данные.

# Консольная составляющая

Для упрощения работы с консолью была использована библиотека CommandLineParser, использующаяся для синтаксического анализа командной строки со стандартизированным стилем \*nix getopt для .NET. Данная библиотека предлагает простой API для управления аргументами командной строки и связанными с ними задачами, такими как определение переключателей, параметров и команд. А также позволяет отображать экран справки с возможностью настройки и простым способом сообщать о синтаксических ошибках конечному пользователю.

Для данного приложения были настроены короткие и полные имена параметров, признаки обязательности свойств (Required), стандартные значение (Default), используемые в случаях, когда параметр не заполнен, а также тексты подсказок (HelpText).

Пример настройки параметров для запуска генератора:
```
 class Options
 {
   [Option('t', "type", Required = true, HelpText = "Type of Sql (Postgres - p, MS - m).")]
   public string SqlType { get; set; }

   [Option('s', "server", Default = "localhost", HelpText = "Server address.")]
   public string Server { get; set; }

   [Option("port", Default = "5432", HelpText = "Postgres server port.")]
   public string Port { get; set; }

   [Option('d', "database", Required = true, HelpText = "Name of database.")]
   public string Database { get; set; }

   [Option('u', "user", Required = true, HelpText = "Database user login.")]
   public string UserId { get; set; }

   [Option('p', "password", Required = true, HelpText = "Database user password.")]
   public string Password { get; set; }

   [Option("emp", Required = true, HelpText = "Count of generated employees.")]
   public int EmployeesCount { get; set; }

   [Option("bus", Required = true, HelpText = "Count of generated businesses.")]
   public int BusinessCount { get; set; }

   [Option("dep", Required = true, HelpText = "Count of generated departments")]
   public int DepartmentsCount { get; set; }
 }
```

В ходе обработки параметров избавляемся от кавычек, использующихся для экранирования, а также проверяем что значения целочисленных параметров больше 0 и выбран правильный тип базы данных. Ниже показан код для проверки введенных пользователем параметров:
```
 static void PreparationToStart(Options opts)
 {
   var types = opts.GetType().GetProperties();

   foreach (var type in types)
   {
     if (type.PropertyType == typeof(string))
       type.SetValue(opts, type.GetValue(opts).ToString().Trim('"'));
     else if (type.PropertyType == typeof(int) && (int)type.GetValue(opts) \< 1)
     {
       Console.WriteLine(\$"Error: {type.Name} should be more than 0");
       return;
     }
   } 

   if (opts.SqlType != "p" && opts.SqlType != "m")
   {
     Console.WriteLine("Error: Wrong type of Sql");
     return;
   }
 …
```

Программа рассчитана на работу с базами данных Postgres и Microsoft SQL, для этого в зависимости от параметра (-t, --type) идет генерация нужной строки подключения.
```
 sqlType = opts.SqlType == "p" ? sqlTypes.Postgres : sqlTypes.MS;

 connectionString = sqlType == sqlTypes.Postgres ?
 \$"Server={opts.Server}; Port={opts.Port}; Database={opts.Database}; Uid={opts.UserId}; Pwd={opts.Password}" :
 \$"Server={opts.Server}; Initial Catalog={opts.Database}; User Id={opts.UserId}; Password={opts.Password}";
```

# Процесс генерации

Программа генерирует и заполняет данные по организациям, подразделениям, персонам, учетным записям и сотрудникам. В соответствии с перечисленными сущностями были созданы модели, содержащие основную информацию. Каждая модель содержит свойство Discriminator, являющееся идентификатором сущности в среде разработки.

Пример модели персоны:
```
 class Person
 {
   public int Id { get; set; }
   public Guid Discriminator = Guid.Parse("f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6");
   public string Status = "Active";
   public string Phone { get; set; }
   public int Code { get; set; }
   public string Lastname { get; set; }
   public string Firstname { get; set; }
   public string Name { get; set; }
   public DateTime Dateofbirth { get; set; }
   public Name.Gender Sex { get; set; }
   public string Shortname { get; set; }
   public string Login { get; set; }
   public string Email { get; set; }
 }
```

Генерация данных происходит при помощи библиотеки с открытым исходным кодом Bogus, являющейся портом популярной библиотеки faker.js на языки .NET. Для заполнения данных используются правила для каждого свойства модели, в которых выбирается набор данных и метод для получения нужного значения. Пример правила генерации структуры персоны:
```
 var genders = new List\<Name.Gender\>() { Name.Gender.Male, Name.Gender.Female }; 

 var newPersons = new Faker\<Person\>("ru")
   .RuleFor(u =\> u.Id, (f, u) =\> f.IndexFaker + maxId)
   .RuleFor(u =\> u.Phone, (f, u) =\> "+7" + f.Phone.PhoneNumber().ToString())
   .RuleFor(u =\> u.Code, (f, u) =\> f.Random.Number(10, 100000))
   .RuleFor(u =\> u.Sex, f =\> f.PickRandom(genders))
   .RuleFor(u =\> u.Lastname, (f, u) =\> f.Name.LastName(u.Sex))
   .RuleFor(u =\> u.Firstname, (f, u) =\> f.Name.FirstName(u.Sex))
   .RuleFor(u =\> u.Name, (f, u) =\> u.Lastname + " " + u.Firstname)
   .RuleFor(u =\> u.Dateofbirth, (f, u) =\> f.Date.Between(DateTime.Today.AddYears(-60), DateTime.Today.AddYears(-18)))
   .RuleFor(u =\> u.Shortname, (f, u) =\> u.Lastname + " " + u.Firstname.Substring(0, 1) + " ")
   .RuleFor(u =\> u.Login, (f, u) =\> f.Internet.UserName(\$"{u.Firstname}{u.Id}", u.Lastname))
   .RuleFor(u =\> u.Email, (f, u) =\> f.Internet.Email(\$"{u.Firstname}{u.Id}", u.Lastname));
```

Перед созданием данных происходит получение следующего за максимальным ID из указанной таблицы в методе GetNextId(). Далее идет заполнение строки SQL запроса, с данными, которые сгенерировались по правилу:
```
 var query = \$"INSERT INTO {tableName}" + "(Id, discriminator, status, name, phones, code, lastname, firstname, dateofbirth, sex, shortname)" + "VALUES ";
 var persons = new List\<Person\>();

 foreach (var person in newPersons.Generate(count))
 {
   query += \$"\\n({person.Id}, '{person.Discriminator}', '{person.Status}', '{person.Name}', '{person.Phone}', " + \$"'{person.Code}', '{person.Lastname}', '{person.Firstname}', '{person.Dateofbirth}', '{person.Sex}', '{person.Shortname}'),";
   persons.Add(person);
 }

 query = query.Substring(0, query.Length - 1);
```

После заполнения строки запроса идет обращение к методу InsertData() в котором происходит выполнение переданной команды. Особенностью структуры данных в базе DirectumRX является, то, что при генерации ID для всех объектов системы RX обращается к таблице sungero_system_ids, поэтому добавляя записи через SQL запрос необходимо обновлять значения в данной таблице. В связи с этим после вставки значений в таблицу идет вызов метода UpdateId(), который обновляет данные в таблице связанной с генерацией ID.

Также при генерации было учтено, что в разных версиях RX часть полей различается, так, например при создании записей сотрудников проверяется наличие в таблице столбца, отвечающего за хранение информации об уведомлениях о текущих заданиях и задачах в виде сводки, до версии RX 4.4 этого поля не было. В связи с этим перед заполнением строки запроса идет обращение к методу IsContainsColumn(), который проверяет наличие указанного столбца.
```
 var falseType = sqlType == sqlTypes.Postgres ? "false" : "0";
 var isNeedNotifySum = IsContainsColumn(command, tableName, "neednotifysuma_company_sungero");
 var needNotifySumStr = isNeedNotifySum ? ", neednotifysuma_company_sungero" : string.Empty;
 var needNotifySumValue = isNeedNotifySum ? \$", {falseType}" : string.Empty;
 var query = \$"INSERT INTO {tableName}" + "(Id, sid, discriminator, status, name, person_company_sungero, login, department_company_sungero, persnumber_company_sungero, " + \$"email_company_sungero, neednotifyexpi_company_sungero, neednotifynewa_company_sungero{needNotifySumStr})" + "VALUES ";
```

После создания сотрудников, необходимо сопоставить ID подразделения сотрудника и ID сотрудника в таблице sungero_core_recipientlink, без этого в карточке подразделения на вкладке сотрудники не будет записей. Для выполнения данных действий используется метод GenerateRecipientLink().

# Пример работы

При запуске приложения появляется справка, с описанием всех параметров.

![image](https://drive.google.com/uc?export=view&id=1zuFdrFLDOAMcr2_AxiCUkFcqYQ4qHQoO)

| Параметр       | Обозначение                                                                             |
|----------------|-----------------------------------------------------------------------------------------|
| -t, --type     | Тип базы данных, Postgres или Microsoft SQL. Варианты заполнения p и m, соответственно. |
| -s, --server   | Адрес сервера. Например "192.168.3.270"                                                 |
| --port         | Порт, используется только для Postgres.                                                 |
| -d, --database | Наименование базы данных.                                                               |
| -u, --user     | Логин для подключения к базе данных.                                                    |
| -p, --password | Пароль для указанного логина.                                                           |
| --emp          | Количество генерируемых сотрудников.                                                    |
| --bus          | Количество генерируемых организаций.                                                    |
| --dep          | Количество генерируемых подразделений.                                                  |
| --help         | Вызов справки.                                                                          |
| --version      | Просмотр версии программы.                                                              |

Пример запуска генерации 3000 сотрудников, с 2 организациями и 10 подразделениями:

![image](https://drive.google.com/uc?export=view&id=1Tqss6oc3cISfc6Z0e69qcF25kUnjj3Zc)

Очередность создания сущностей:

1.  Организации;
2.  Подразделения, случайно распределенные между созданными организациями;
3.  Персоны;
4.  Учетные записи;
5.  Сотрудники, случайно распределенные между созданными подразделениями.

После успешного завершения процесса в системе появляются новые записи.

Организация:

![image](https://drive.google.com/uc?export=view&id=1Xj2n1Qu_dcaSP9Svn4MXcZMcZJsFaMX-)

Подразделение:

![image](https://drive.google.com/uc?export=view&id=1TwO9kHgSBYZd3p2hJyDpsEPPlCtS-eG9)

![image](https://drive.google.com/uc?export=view&id=11QElFUm08NAYRBVhIEXrvFmwT35PHkPo)

Сотрудник:

![image](https://drive.google.com/uc?export=view&id=1Ua971nuvsB-1k_ZwLu5wQcyVaRNNd9xn)

Персона:

![image](https://drive.google.com/uc?export=view&id=1DYSudayxlgXgSe7olTMmfT0n_XKaDuZj)

# Общие сведения

1.  Для ускорения работы программы, генерация и запись сотрудников в базу происходит пакетами до 1000 сотрудников;
2.  При записи данных в базу используется транзакция. Так при возникновении ошибок происходит откат транзакции и лишние данные не попадают в базу.

# Заключение

В дальнейшем планируется:

1.  Доработать загрузку логинов, для назначения стандартного пароля вместо внешней аутентификации;
2.  Реализовать графический интерфейс, для большего удобства и дальнейшего расширения функционала;
3.  Добавить возможность выбора нужных полей из таблиц и заполнения их различными данными.
