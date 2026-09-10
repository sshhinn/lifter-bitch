using UnityEngine;

/// <summary>
/// Управляет "поездкой" в лифте:
/// 1. Проигрывает длинную аудиодорожку с правилами дня.
/// 2. Как только она заканчивается — проигрывает короткий звук "дзынь".
/// 3. Двери плавно раздвигаются в стороны (открываются).
/// 4. Постояв открытыми заданное время, двери плавно закрываются обратно.
/// </summary>
public class ElevatorRideController : MonoBehaviour
{
    [Header("Двери лифта")]
    [Tooltip("Transform левой створки двери")]
    public Transform leftDoor;

    [Tooltip("Transform правой створки двери")]
    public Transform rightDoor;

    [Header("Направление открытия (в ЛОКАЛЬНЫХ координатах створки)")]
    [Tooltip("Куда должна поехать левая створка при открытии. Обычно (-1, 0, 0) или (1, 0, 0) — смотри инструкцию по настройке.")]
    public Vector3 leftDoorOpenDirection = Vector3.left;

    [Tooltip("Куда должна поехать правая створка при открытии. Обычно противоположно левой.")]
    public Vector3 rightDoorOpenDirection = Vector3.right;

    [Header("Параметры дверей")]
    [Tooltip("Скорость движения створок (единиц в секунду) — общая и для открытия, и для закрытия")]
    public float doorSpeed = 1.5f;

    [Tooltip("На какое расстояние должна отъехать каждая створка от закрытого положения")]
    public float doorOpenDistance = 1.0f;

    [Tooltip("Сколько секунд двери стоят полностью открытыми, прежде чем начнут закрываться")]
    public float doorOpenHoldTime = 3.0f;

    [Header("Звук")]
    [Tooltip("AudioSource, на котором уже стоит длинный клип поездки (правила дня)")]
    public AudioSource rideAudioSource;

    [Tooltip("Короткий звук 'дзынь' — проигрывается один раз, когда поездка закончилась")]
    public AudioClip dingSound;

    /// <summary>
    /// Все возможные состояния дверей. Простая state-machine вместо кучи отдельных bool-флагов —
    /// в каждый момент времени двери находятся ровно в одном из этих состояний.
    /// </summary>
    private enum DoorState
    {
        Closed,      // двери закрыты и ничего не происходит (стартовое состояние)
        Opening,     // двери едут в открытое положение
        OpenWaiting, // двери полностью открыты, идёт отсчёт времени перед закрытием
        Closing      // двери едут обратно в закрытое положение
    }

    private DoorState doorState = DoorState.Closed;

    // Сколько секунд уже прошло с момента, как двери полностью открылись
    private float openWaitTimer = 0f;

    // Закрытые (стартовые) позиции створок — запоминаем при старте
    private Vector3 leftDoorClosedPos;
    private Vector3 rightDoorClosedPos;

    // Открытые позиции створок — вычисляются при старте
    private Vector3 leftDoorOpenPos;
    private Vector3 rightDoorOpenPos;

    // Флаг: аудиодорожка поездки уже реально начала играть.
    // Нужен, чтобы не спутать "ещё не началось" с "уже закончилось" в первом же кадре.
    private bool rideHasStarted = false;

    // Флаг: обработали ли мы момент окончания поездки (чтобы не сработало повторно)
    private bool rideFinishedHandled = false;

    private void Start()
    {
        // Запоминаем исходные (закрытые) локальные позиции створок
        if (leftDoor != null)
        {
            leftDoorClosedPos = leftDoor.localPosition;
            leftDoorOpenPos = leftDoorClosedPos + leftDoorOpenDirection.normalized * doorOpenDistance;
        }

        if (rightDoor != null)
        {
            rightDoorClosedPos = rightDoor.localPosition;
            rightDoorOpenPos = rightDoorClosedPos + rightDoorOpenDirection.normalized * doorOpenDistance;
        }

        // Запускаем длинную дорожку поездки, если она ещё не играет
        if (rideAudioSource != null && !rideAudioSource.isPlaying)
        {
            rideAudioSource.Play();
        }
    }

    private void Update()
    {
        TrackRideAudioProgress();
        UpdateDoors();
    }

    /// <summary>
    /// Следит за тем, играет ли ещё длинная дорожка поездки.
    /// Как только AudioSource перестал играть (клип доиграл до конца) — считаем поездку завершённой.
    /// </summary>
    private void TrackRideAudioProgress()
    {
        if (rideFinishedHandled || rideAudioSource == null)
        {
            return;
        }

        // Ждём, пока аудио реально начнёт играть, чтобы не спутать
        // "клип ещё не запустился" с "клип уже закончился" в самом первом кадре
        if (!rideHasStarted)
        {
            if (rideAudioSource.isPlaying)
            {
                rideHasStarted = true;
            }
            return;
        }

        // Если дорожка началась, но сейчас больше не играет — значит, она закончилась
        if (!rideAudioSource.isPlaying)
        {
            rideFinishedHandled = true;
            OnRideFinished();
        }
    }

    /// <summary>
    /// Вызывается один раз, когда длинная дорожка поездки закончилась.
    /// Проигрывает звук "дзынь" и запускает открытие дверей.
    /// </summary>
    private void OnRideFinished()
    {
        // Проигрываем короткий звук прибытия через тот же AudioSource,
        // не прерывая его основной клип (PlayOneShot можно вызывать даже если AudioSource сейчас не играет)
        if (rideAudioSource != null && dingSound != null)
        {
            rideAudioSource.PlayOneShot(dingSound);
        }

        doorState = DoorState.Opening;
    }

    /// <summary>
    /// Главный "мозг" дверей — в зависимости от текущего состояния
    /// либо двигает створки, либо просто отсчитывает время ожидания.
    /// </summary>
    private void UpdateDoors()
    {
        switch (doorState)
        {
            case DoorState.Opening:
                MoveDoorsTowards(leftDoorOpenPos, rightDoorOpenPos);
                if (BothDoorsReached(leftDoorOpenPos, rightDoorOpenPos))
                {
                    // Двери только что полностью открылись — начинаем отсчёт перед закрытием
                    doorState = DoorState.OpenWaiting;
                    openWaitTimer = 0f;
                }
                break;

            case DoorState.OpenWaiting:
                openWaitTimer += Time.deltaTime;
                if (openWaitTimer >= doorOpenHoldTime)
                {
                    doorState = DoorState.Closing;
                }
                break;

            case DoorState.Closing:
                MoveDoorsTowards(leftDoorClosedPos, rightDoorClosedPos);
                if (BothDoorsReached(leftDoorClosedPos, rightDoorClosedPos))
                {
                    doorState = DoorState.Closed;
                }
                break;

            case DoorState.Closed:
            default:
                // В закрытом состоянии двигать нечего — ждём следующей команды (например, новой поездки)
                break;
        }
    }

    /// <summary>
    /// Двигает обе створки к заданным целевым позициям на один шаг (Vector3.MoveTowards).
    /// Используется и для открытия, и для закрытия — просто с разными целями.
    /// </summary>
    private void MoveDoorsTowards(Vector3 leftTargetPos, Vector3 rightTargetPos)
    {
        float step = doorSpeed * Time.deltaTime;

        if (leftDoor != null)
        {
            leftDoor.localPosition = Vector3.MoveTowards(leftDoor.localPosition, leftTargetPos, step);
        }

        if (rightDoor != null)
        {
            rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, rightTargetPos, step);
        }
    }

    /// <summary>
    /// Проверяет, доехали ли ОБЕ створки до указанных целевых позиций.
    /// </summary>
    private bool BothDoorsReached(Vector3 leftTargetPos, Vector3 rightTargetPos)
    {
        bool leftDone = leftDoor == null || leftDoor.localPosition == leftTargetPos;
        bool rightDone = rightDoor == null || rightDoor.localPosition == rightTargetPos;
        return leftDone && rightDone;
    }
}
