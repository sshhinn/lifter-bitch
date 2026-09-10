using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Контроллер субтитров: показывает текст с эффектом "печатной машинки"
/// (буквы появляются по одной), держит его на экране заданное время,
/// а потом скрывает.
/// Другие скрипты вызывают публичный метод ShowSubtitle(...), чтобы показать реплику.
/// </summary>
public class SubtitleController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Текстовый элемент (TextMeshPro), в котором будут отображаться субтитры")]
    [SerializeField] private TMP_Text subtitleText;

    [Tooltip("Необязательно: объект-панель субтитров (фон под текстом). Если есть — будет включаться/выключаться вместе с текстом. Можно оставить пустым.")]
    [SerializeField] private GameObject subtitlePanel;

    [Header("Настройки появления букв")]
    [Tooltip("Задержка между появлением каждой буквы (в секундах). Чем меньше значение — тем быстрее печатает текст.")]
    [SerializeField] private float letterDelay = 0.03f;

    [Header("Настройки показа")]
    [Tooltip("Сколько секунд субтитры остаются на экране ПОСЛЕ того, как весь текст напечатался (до того, как исчезнуть)")]
    [SerializeField] private float displayDuration = 3f;

    // Ссылка на текущую запущенную корутину.
    // Нужна, чтобы можно было остановить предыдущие субтитры, если запускаются новые до того, как старые закончились.
    private Coroutine currentSubtitleRoutine;

    private void Awake()
    {
        // В самом начале скрываем субтитры, чтобы пустое поле или панель не "торчали" на экране
        HideSubtitleInstant();
    }

    /// <summary>
    /// Публичный метод для показа субтитров.
    /// Вызывай его из любого другого скрипта, когда нужно показать реплику,
    /// например: subtitleController.ShowSubtitle("Кто-то стучит в дверь лифта...");
    /// </summary>
    public void ShowSubtitle(string text)
    {
        // Если субтитры уже показываются (например, предыдущая реплика ещё не закончилась) —
        // останавливаем старую корутину, чтобы не было наложения текста
        if (currentSubtitleRoutine != null)
        {
            StopCoroutine(currentSubtitleRoutine);
        }

        currentSubtitleRoutine = StartCoroutine(SubtitleRoutine(text));
    }

    /// <summary>
    /// Корутина, которая печатает текст по буквам, держит его на экране, а потом скрывает.
    /// </summary>
    private IEnumerator SubtitleRoutine(string text)
    {
        // Включаем панель субтитров, если она назначена
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(true);
        }

        // Очищаем текст перед началом печати
        subtitleText.text = "";

        // Печатаем текст по одной букве.
        // Проходим по каждому символу строки и добавляем его к уже показанному тексту.
        foreach (char letter in text)
        {
            subtitleText.text += letter;
            yield return new WaitForSeconds(letterDelay);
        }

        // Текст полностью напечатан — ждём, пока пройдёт время показа
        yield return new WaitForSeconds(displayDuration);

        // Скрываем субтитры
        HideSubtitleInstant();

        currentSubtitleRoutine = null;
    }

    /// <summary>
    /// Мгновенно очищает текст и выключает панель субтитров (без анимации).
    /// </summary>
    private void HideSubtitleInstant()
    {
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }

        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }
}
