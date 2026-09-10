using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер 2D-панели выбора этажей для лифта.
/// Скрипт нужно повесить на объект панели (или на пустой GameObject-менеджер).
/// </summary>
public class ElevatorUIController : MonoBehaviour
{
    [Header("Настройки поездки")]
    [Tooltip("Этаж, на котором лифт находится в самом начале игры")]
    [SerializeField] private int currentFloor = 1;

    [Tooltip("Сколько секунд лифт едет между ДВУМЯ СОСЕДНИМИ этажами (например, с 1 на 2)")]
    [SerializeField] private float secondsPerFloor = 1.5f;

    [Tooltip("Минимальная длительность поездки в секундах (даже если едем всего на 1 этаж)")]
    [SerializeField] private float minRideDuration = 2f;

    [Header("Табло этажа")]
    [Tooltip("Текстовый элемент табло (TextMeshPro), который показывает текущий этаж лифта. Подходит и обычный TextMeshPro в 3D-сцене, и TextMeshProUGUI на Canvas.")]
    [SerializeField] private TMP_Text floorDisplayText;

    [Header("Субтитры")]
    [Tooltip("Ссылка на скрипт SubtitleController, который показывает субтитры на экране")]
    [SerializeField] private SubtitleController subtitleController;

    [Header("Кнопки этажей")]
    [Tooltip("Сюда перетащи ВСЕ кнопки этажей с панели, чтобы скрипт мог их блокировать/разблокировать")]
    [SerializeField] private Button[] floorButtons;

    [Header("Звуки")]
    [Tooltip("Источник звука, из которого будут проигрываться звуки лифта")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Звук движения лифта (проигрывается всё время поездки)")]
    [SerializeField] private AudioClip rideSound;

    [Tooltip("Звук 'дзынь' при прибытии на этаж")]
    [SerializeField] private AudioClip arrivalSound;

    // Флаг, который показывает, едет лифт сейчас или нет.
    // Нужен, чтобы случайно не запустить вторую поездку поверх первой.
    private bool isMoving = false;

    /// <summary>
    /// Публичное свойство "только для чтения" — позволяет другим скриптам
    /// узнать, на каком этаже сейчас находится лифт, не давая им менять это значение напрямую.
    /// </summary>
    public int CurrentFloor => currentFloor;

    /// <summary>
    /// Вызывается один раз при старте сцены.
    /// Показываем на табло тот этаж, с которого лифт начинает игру.
    /// </summary>
    private void Start()
    {
        UpdateFloorDisplay(currentFloor);
    }

    /// <summary>
    /// Этот метод нужно привязать к событию OnClick() каждой кнопки этажа в инспекторе Unity.
    /// floorNumber — номер этажа, на который нужно поехать (например, 1, 2, 13).
    /// </summary>
    public void OnFloorButtonClicked(int floorNumber)
    {
        // Защита: если лифт уже едет, игнорируем повторное нажатие.
        // (На всякий случай, даже если кнопки уже неактивны)
        if (isMoving)
        {
            return;
        }

        // Запускаем корутину поездки на нужный этаж
        StartCoroutine(RideToFloor(floorNumber));
    }

    /// <summary>
    /// Корутина, которая симулирует поездку лифта.
    /// </summary>
    private IEnumerator RideToFloor(int floorNumber)
    {
        // Если игрок нажал кнопку того этажа, на котором лифт уже стоит —
        // никуда ехать не нужно, просто открываем двери сразу.
        if (floorNumber == currentFloor)
        {
            Debug.Log("Лифт уже находится на этаже: " + floorNumber);
            TriggerOpenDoors();
            yield break; // Выходим из корутины, дальше код не выполняется
        }

        isMoving = true;

        // Выводим в консоль информацию о том, куда едет лифт
        Debug.Log("Лифт отправляется на этаж: " + floorNumber);

        // Блокируем все кнопки, чтобы игрок не мог нажать другой этаж во время поездки
        SetButtonsInteractable(false);

        // Проигрываем звук поездки
        PlaySound(rideSound);

        // Считаем, сколько этажей нужно проехать (расстояние между текущим и целевым этажом).
        // Mathf.Abs нужен, чтобы не важно было, едем мы вверх или вниз — расстояние всегда положительное.
        int floorsToTravel = Mathf.Abs(floorNumber - currentFloor);

        // Длительность поездки = количество этажей * время на один этаж.
        // Mathf.Max гарантирует, что поездка не будет короче minRideDuration,
        // даже если едем всего на 1 этаж.
        float rideDuration = Mathf.Max(minRideDuration, floorsToTravel * secondsPerFloor);

        // Делим общую длительность поездки поровну между всеми этажами, которые нужно проехать.
        // Так табло будет переключаться равномерно, даже если сработал minRideDuration.
        float stepDuration = rideDuration / floorsToTravel;

        // Направление движения: +1, если едем вверх, -1, если вниз.
        int direction = floorNumber > currentFloor ? 1 : -1;

        // Шагаем по одному этажу за раз, каждый раз ожидая stepDuration секунд,
        // и обновляем табло — так цифра будет "пролистываться" 1 → 2 → 3 и т.д.
        for (int i = 0; i < floorsToTravel; i++)
        {
            yield return new WaitForSeconds(stepDuration);

            currentFloor += direction;
            UpdateFloorDisplay(currentFloor);
        }

        // --- Лифт "приехал" (currentFloor теперь точно равен floorNumber) ---

        // Останавливаем звук поездки, даже если сам клип ещё не доиграл до конца.
        // Это нужно, чтобы гул мотора не накладывался на звук "дзынь".
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Проигрываем звук прибытия ("дзынь") — теперь уже после того, как звук поездки точно затих
        PlaySound(arrivalSound);

        // Разблокируем кнопки обратно
        SetButtonsInteractable(true);

        // Вызываем метод открытия дверей (заглушка — логику допишешь сам)
        TriggerOpenDoors();

        isMoving = false;
    }

    /// <summary>
    /// Включает или выключает интерактивность (нажимаемость) всех кнопок этажей.
    /// </summary>
    private void SetButtonsInteractable(bool state)
    {
        // Проходим по всем кнопкам в массиве и меняем их свойство interactable
        foreach (Button button in floorButtons)
        {
            if (button != null)
            {
                button.interactable = state;
            }
        }
    }

    /// <summary>
    /// Проигрывает переданный звуковой клип через AudioSource.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        // Проверяем, что и AudioSource, и клип назначены, чтобы не было ошибок
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Обновляет текст на табло этажа.
    /// </summary>
    private void UpdateFloorDisplay(int floor)
    {
        // Проверяем, что табло вообще назначено в инспекторе — если нет, просто ничего не делаем
        if (floorDisplayText != null)
        {
            floorDisplayText.text = floor.ToString();
        }
    }

    /// <summary>
    /// Заглушка метода открытия дверей.
    /// Сюда позже нужно вставить свою логику (анимация дверей, звук открытия и т.д.)
    /// </summary>
    private void TriggerOpenDoors()
    {
        Debug.Log("Двери лифта открываются... (сюда нужно вставить свою логику)");

        // Вот он — вызов метода из ДРУГОГО скрипта.
        // Мы обращаемся к переменной subtitleController (ссылка на объект с SubtitleController)
        // и вызываем на ней публичный метод ShowSubtitle, передавая нужный текст.
        if (subtitleController != null)
        {
            subtitleController.ShowSubtitle("Лифт прибыл на этаж " + currentFloor);
        }
    }
}
