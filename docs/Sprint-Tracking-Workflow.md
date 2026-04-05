# Sprint tracking workflow

Дата: 2026-04-05

Чтобы синхронизировать `docs/Sprint-Remaining-Tasks-2026-04-05.md` после каждого task/PR:

1. Обновить соответствующий task в самом документе (чекбокс и краткий статус).
2. Добавить короткую запись в секцию `### Текущий прогресс` через helper-скрипт:

```bash
python scripts/update-sprint-remaining.py --task-id <task-id> --note "<короткое описание>" --status done
```

3. Проверить diff и убедиться, что запись имеет UTC timestamp и не ломает структуру markdown.

Пример:

```bash
python scripts/update-sprint-remaining.py --task-id provider:quality-signals-for-microsoft-support --note "Added hint-type confidence mapping" --status done
```
