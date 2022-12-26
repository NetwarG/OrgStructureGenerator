# Генератор оргструктуры
Решение предназначено для генерации оргструктуры в Directum RX.

При запуске приложения появляется справка, с описанием всех параметров.


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

Пример запуска генерации 100 сотрудников, с 2 организациями и 5 подразделениями:
Postgres
* -t p -d directum -u postgres -p 11111 --emp 100 --bus 2 --dep 5
MS SQL
* -t m -s "192.168.3.270" -d directum -u sa -p 11111 --emp 100 --bus 2 --dep 5

Очередность создания сущностей:

1.  Организации;
2.  Подразделения, случайно распределенные между созданными организациями;
3.  Персоны;
4.  Учетные записи;
5.  Сотрудники, случайно распределенные между созданными подразделениями.

После успешного завершения процесса в системе появляются новые записи.

# Общие сведения

1.  Для ускорения работы программы, генерация и запись сотрудников в базу происходит пакетами до 1000 сотрудников;
2.  При записи данных в базу используется транзакция. Так при возникновении ошибок происходит откат транзакции и лишние данные не попадают в базу.
