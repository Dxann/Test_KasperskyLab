# AwesomeFiles.Client

Консольная утилита-клиент для `AwesomeFilesApi`.

## Запуск

Убедитесь, что backend запущен (по умолчанию: `http://localhost:5073`).

Из папки AwesomeFiles.Client вводим:

```bash
# список команд
dotnet run -- --help

# список файлов
dotnet run -- list

# создать задачу архивации
dotnet run -- create-archive file1.txt file2.txt

# проверить статус
dotnet run -- status <id>

# скачать архив
dotnet run -- download <id> --out <path_to_folder>

# режим "одной командой": создать -> ждать -> скачать
dotnet run -- fetch file1.txt file2.txt --out <path_to_folder>
```

## POSIX-стиль CLI

Команды и параметры реализованы в POSIX-стиле:
- флаги `--base-url`, `--out`
- алиасы `-b`, `-o`
- `--help`
