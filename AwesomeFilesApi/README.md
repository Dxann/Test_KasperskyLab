# Awesome Files API

## Запуск
dotnet restore
dotnet build
dotnet run
По умолчанию сервис запускается на: http://localhost:5073

## Папки
Files/ — входные файлы
Archives/ — готовые архивы

## Swagger
Документация и тестирование API через Swagger:
http://localhost:5073/swagger

GET /api/files – убедитесь, что файлы есть в папке Files

POST /api/archive – добавьте JSON-массив имён файлов для архивирования и нажмите Execute. 
Пример: ["test1.txt", "test2.txt"]

Скопируйте ID задачи из ответа.

GET /api/archive/{id} – проверьте статус архива.

GET /api/archive/{id}/download – скачайте архив, когда статус Completed.

## Логирование
В консоль логируются запросы в формате:
HTTP_METHOD PATH -> STATUS_CODE