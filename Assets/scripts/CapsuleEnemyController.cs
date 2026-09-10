using UnityEngine;

/// <summary>
/// Отслеживает текущий этаж лифта и "выпускает" врага из капсулы,
/// когда лифт прибывает на нужный этаж.
/// </summary>
public class CapsuleEnemyController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Ссылка на скрипт лифта, чтобы узнавать, на каком этаже он сейчас находится")]
    [SerializeField] private ElevatorUIController elevatorController;

    [Header("Настройки")]
    [Tooltip("Номер этажа, на котором враг должен выйти из капсулы")]
    [SerializeField] private int floorToAppear = 12;

    [Header("Движение врага")]
    [Tooltip("Координаты точки, куда враг направится после выхода из капсулы")]
    [SerializeField] private Vector3 targetPosition;

    [Tooltip("Скорость движения врага к целевой точке")]
    [SerializeField] private float moveSpeed = 3f;

    // Флаг нужен, чтобы враг вышел из капсулы ОДИН РАЗ,
    // а не пытался выходить заново на каждом кадре, пока лифт стоит на 12 этаже
    private bool hasAppeared = false;

    void Update()
    {
        // Если враг ещё не вышел — проверяем этаж лифта
        if (!hasAppeared)
        {
            // Если ссылка на лифт не назначена в инспекторе — ничего не делаем (защита от ошибки)
            if (elevatorController == null)
            {
                return;
            }

            // Сравниваем текущий этаж лифта с нужным нам этажом появления
            if (elevatorController.CurrentFloor == floorToAppear)
            {
                ExitCapsule();
                hasAppeared = true;
            }

            return;
        }

        // Враг уже вышел из капсулы — двигаем его к целевой точке.
        // Работает так же, как в скрипте MoveToPoint: на каждом кадре подвигаем
        // объект чуть ближе к targetPosition со скоростью moveSpeed.
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Заглушка метода выхода врага из капсулы.
    /// Сюда позже нужно вставить свою логику (анимация открытия капсулы,
    /// звук, включение модели врага, ИИ и т.д.)
    /// </summary>
    private void ExitCapsule()
    {
        Debug.Log("Капсула открывается — враг выходит на этаже " + floorToAppear);
    }
}