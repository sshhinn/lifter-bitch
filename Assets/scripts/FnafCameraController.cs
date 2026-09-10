using UnityEngine;

/// <summary>
/// Поворот камеры в зависимости от положения курсора мыши на экране — как во FNAF.
/// Игрок стоит на месте, не ходит. Курсор мыши НЕ блокируется и остаётся видимым
/// (чтобы им можно было кликать по кнопкам лифта и запискам).
/// Чем ближе курсор к краю экрана — тем сильнее камера повёрнута в эту сторону.
/// </summary>
public class FnafCameraController : MonoBehaviour
{
    [Header("Чувствительность / плавность поворота")]
    [Tooltip("Скорость, с которой камера \"догоняет\" нужный угол поворота. " +
             "Маленькое значение (1-3) даёт тягучий, ленивый поворот, как во FNAF. " +
             "Большое значение (10+) — почти мгновенный, резкий поворот.")]
    public float rotationSpeed = 2.5f;

    [Header("Ограничения поворота")]
    [Tooltip("Максимальный угол поворота влево/вправо (по горизонтали), в градусах")]
    public float maxYawAngle = 60f;

    [Tooltip("Максимальный угол поворота вверх/вниз (по вертикали), в градусах")]
    public float maxPitchAngle = 30f;

    [Header("Дополнительно")]
    [Tooltip("Включи, если камера крутится вверх/вниз в обратную сторону от движения мыши")]
    public bool invertVertical = false;

    [Header("Управление")]
    [Tooltip("Какую кнопку мыши нужно зажать, чтобы крутить камеру: 0 = левая (ЛКМ), 1 = правая (ПКМ), 2 = средняя. " +
             "Пока кнопка не зажата, камера остаётся неподвижной — мышью можно свободно кликать по кнопкам и запискам.")]
    public int rotateMouseButton = 1;

    // Текущие сглаженные углы поворота (то, что реально применяется к камере каждый кадр)
    private float currentYaw = 0f;
    private float currentPitch = 0f;

    // Поворот камеры, который был выставлен вручную в редакторе (например, камера
    // изначально смотрит внутрь лифта под каким-то своим углом). Поворот от мыши
    // добавляется ПОВЕРХ этого базового поворота, а не заменяет его полностью —
    // иначе камера в момент запуска сцены "прыгнет" в сторону (0,0,0) и собьёт всю сцену.
    private Quaternion baseLocalRotation;

    private void Start()
    {
        // Запоминаем поворот, который камера имеет прямо сейчас в сцене (заданный вручную)
        baseLocalRotation = transform.localRotation;

        // Курсор должен оставаться видимым и свободным (не залипать в центре экрана),
        // чтобы позже им можно было нажимать на кнопки лифта и читать записки
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // 0. Поворачивать камеру можно только пока зажата нужная кнопка мыши (по умолчанию — ПКМ).
        //    Если кнопка не зажата — просто выходим из Update(), ничего не трогая:
        //    камера остаётся ровно там, где была, а мышью можно свободно
        //    водить по экрану и кликать по кнопкам/запискам, не поворачивая при этом камеру.
        if (!Input.GetMouseButton(rotateMouseButton))
        {
            return;
        }

        // 1. Берём позицию курсора мыши в пикселях экрана
        Vector3 mousePos = Input.mousePosition;

        // 2. Переводим координаты мыши в диапазон от -1 до 1 относительно центра экрана.
        //    0 — мышь точно в центре экрана.
        //    -1 — мышь у левого (или нижнего) края, +1 — у правого (или верхнего) края.
        float xNormalized = (mousePos.x / Screen.width - 0.5f) * 2f;
        float yNormalized = (mousePos.y / Screen.height - 0.5f) * 2f;

        // На случай, если курсор ушёл за пределы окна игры — жёстко ограничиваем диапазон
        xNormalized = Mathf.Clamp(xNormalized, -1f, 1f);
        yNormalized = Mathf.Clamp(yNormalized, -1f, 1f);

        // 3. Вычисляем "целевой" угол поворота — куда камера ДОЛЖНА в итоге смотреть
        //    прямо сейчас, исходя из положения мыши
        float targetYaw = xNormalized * maxYawAngle;

        // По умолчанию (invertVertical = false): мышь у верхнего края экрана -> камера смотрит вверх.
        // Поэтому знак минус — так как в Unity отрицательный угол по оси X = взгляд вверх.
        float verticalSign = invertVertical ? 1f : -1f;
        float targetPitch = yNormalized * maxPitchAngle * verticalSign;

        // 4. Плавно "подтягиваем" текущий угол к целевому — именно это даёт
        //    тот самый тягучий, инерционный эффект поворота головы, как во FNAF
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationSpeed * Time.deltaTime);

        // 5. Дополнительная жёсткая проверка на всякий случай — угол никогда
        //    не должен выходить за заданные пределы
        currentYaw = Mathf.Clamp(currentYaw, -maxYawAngle, maxYawAngle);
        currentPitch = Mathf.Clamp(currentPitch, -maxPitchAngle, maxPitchAngle);

        // 6. Применяем итоговый поворот к камере — ОТНОСИТЕЛЬНО базового поворота,
        //    заданного в редакторе (baseLocalRotation), а не абсолютно.
        //    Pitch (наклон вверх/вниз) — вращение вокруг оси X.
        //    Yaw (поворот влево/вправо) — вращение вокруг оси Y.
        transform.localRotation = baseLocalRotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
    }
}
