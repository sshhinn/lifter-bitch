using UnityEngine;

/// <summary>
/// Двигает объект к целевой точке.
/// Когда объект доезжает — один раз показывает субтитры.
/// </summary>
public class MoveToPoint : MonoBehaviour
{
    public Vector3 targetPosition; // Целевые координаты
    public float speed = 5f;       // Скорость движения

    [Header("Субтитры")]
    [Tooltip("Ссылка на скрипт SubtitleController, который показывает субтитры на экране")]
    [SerializeField] private SubtitleController subtitleController;

    [Tooltip("Текст, который покажется, когда объект доедет до цели")]
    [SerializeField] private string arrivalSubtitleText = "Твой текст субтитров";

    // Флаг нужен, чтобы субтитры показались ОДИН РАЗ, а не на каждом кадре,
    // пока объект стоит на месте после прибытия
    private bool hasArrived = false;

    void Update()
    {
        // Если объект уже доехал — дальше в Update делать нечего, выходим
        if (hasArrived)
        {
            return;
        }

        // Передвигаем объект от текущей позиции к целевой
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Проверяем, доехали ли мы до цели.
        // Сравнивать позиции через == почти никогда не сработает точно из-за погрешностей float,
        // поэтому проверяем через Vector3.Distance с маленьким порогом (0.01f).
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget < 0.01f)
        {
            // Объект доехал — "скрипт закончился" в твоей формулировке.
            // Вот тут и происходит вызов метода из ДРУГОГО скрипта (SubtitleController):
            if (subtitleController != null)
            {
                subtitleController.ShowSubtitle(arrivalSubtitleText);
            }

            // Поднимаем флаг, чтобы это условие больше не срабатывало повторно
            hasArrived = true;
        }
    }
}
