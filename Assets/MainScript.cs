using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Параметры настройки
    [SerializeField] private int wheatPerPeasant = 15;   // Сколько пшеницы приносит каждый крестьянин
    [SerializeField] private float wheatTimerInterval = 15f;  // Интервал между сбором пшеницы
    [SerializeField] private int wheatConsumptionRate = 1;    // Скорость потребления пшеницы воинами
    [SerializeField] private float consumptionTimerInterval = 30f;  // Интервал между проверками потребления
    [SerializeField] private int peasantHireCost = 20;     // Стоимость найма крестьянина
    [SerializeField] private int warriorHireCost = 50;     // Стоимость найма воина
    [SerializeField] private float hireTimerDuration = 10f;  // Длительность процесса найма
    [SerializeField] private float attackWaveInterval = 60f;  // Интервал между волнами атаки
    [SerializeField] private int initialEnemyCount = 2;      // Начальное число врагов
    [SerializeField] private int victoryWheatThreshold = 100; // Пороговое значение пшеницы для победы
    [SerializeField] private int victoryPopulationThreshold = 25; // Пороговое значение населения для победы

    // Элементы интерфейса
    public TMP_Text wheatText;
    public TMP_Text peasantsText;
    public TMP_Text warriorsText;
    public TMP_Text nextAttackText;
    public Slider wheatCollectionSlider;
    public Slider wheatConsumptionSlider;
    public Slider peasantHiringSlider;
    public Slider warriorHiringSlider;
    public Button hirePeasantButton;
    public Button hireWarriorButton;
    public GameObject winPopup;
    public GameObject losePopup;
    
    // Внутренние переменные
    private int currentWheat;
    private int currentPeasants;
    private int currentWarriors;
    private int enemiesNextWave;
    private bool isPeasantHiring;
    private bool isWarriorHiring;
    private Coroutine wheatCollectionRoutine;
    private Coroutine wheatConsumptionRoutine;
    private Coroutine enemyAttackRoutine;
    private Coroutine peasantHiringRoutine;
    private Coroutine warriorHiringRoutine;

    void Start()
    {
        // Начальные значения
        currentWheat = 0;
        currentPeasants = 5;
        currentWarriors = 2;
        enemiesNextWave = initialEnemyCount;
        
        UpdateUI();
        
        // Запускаем основные таймеры
        wheatCollectionRoutine = StartCoroutine(WheatCollection());
        wheatConsumptionRoutine = StartCoroutine(WheatConsumption());
        enemyAttackRoutine = StartCoroutine(EnemyAttack());
    }

    void Update()
    {
        // Обновляем индикаторы прогресса
        if (wheatCollectionSlider != null)
            wheatCollectionSlider.value = Mathf.PingPong(Time.time, wheatTimerInterval) / wheatTimerInterval;
        if (wheatConsumptionSlider != null)
            wheatConsumptionSlider.value = Mathf.PingPong(Time.time, consumptionTimerInterval) / consumptionTimerInterval;
    }

    // Метод обновления интерфейса
    private void UpdateUI()
    {
        wheatText.text = $"Пшеница: {currentWheat}";
        peasantsText.text = $"Крестьяне: {currentPeasants}";
        warriorsText.text = $"Воины: {currentWarriors}";
        nextAttackText.text = $"Следующая атака: {enemiesNextWave} врагов";

        // Активность кнопок найма
        hirePeasantButton.interactable = !isPeasantHiring && currentWheat >= peasantHireCost;
        hireWarriorButton.interactable = !isWarriorHiring && currentWheat >= warriorHireCost;
    }

    // Корутина для сбора пшеницы
    IEnumerator WheatCollection()
    {
        while (true)
        {
            yield return new WaitForSeconds(wheatTimerInterval);
            AddWheat(currentPeasants * wheatPerPeasant);
        }
    }

    // Корутина для потребления пшеницы воинами
    IEnumerator WheatConsumption()
    {
        while (true)
        {
            yield return new WaitForSeconds(consumptionTimerInterval);
            ConsumeWheat(currentWarriors * wheatConsumptionRate);
        }
    }

    // Корутина для волны атак врагов
    IEnumerator EnemyAttack()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackWaveInterval);
            Attack(enemiesNextWave);
            enemiesNextWave += Random.Range(-2, 4); // Можно варьировать сложность
            if (enemiesNextWave < 1) enemiesNextWave = 1;
        }
    }

    // Корутина для найма крестьянина
    IEnumerator HirePeasant()
    {
        isPeasantHiring = true;
        hirePeasantButton.interactable = false;
        peasantHiringSlider.gameObject.SetActive(true);
        peasantHiringSlider.value = 0;

        for (float t = 0; t < hireTimerDuration; t += Time.deltaTime)
        {
            peasantHiringSlider.value = t / hireTimerDuration;
            yield return null;
        }

        currentPeasants++;
        isPeasantHiring = false;
        peasantHiringSlider.gameObject.SetActive(false);
        UpdateUI();
    }

    // Корутина для найма воина
    IEnumerator HireWarrior()
    {
        isWarriorHiring = true;
        hireWarriorButton.interactable = false;
        warriorHiringSlider.gameObject.SetActive(true);
        warriorHiringSlider.value = 0;

        for (float t = 0; t < hireTimerDuration; t += Time.deltaTime)
        {
            warriorHiringSlider.value = t / hireTimerDuration;
            yield return null;
        }

        currentWarriors++;
        isWarriorHiring = false;
        warriorHiringSlider.gameObject.SetActive(false);
        UpdateUI();
    }

    // Добавить пшеницу
    private void AddWheat(int amount)
    {
        currentWheat += amount;
        UpdateUI();
        CheckVictoryConditions();
    }

    // Потратить пшеницу
    private void ConsumeWheat(int amount)
    {
        if (amount > currentWheat)
        {
            int deadWarriors = (amount - currentWheat) / wheatConsumptionRate;
            KillWarriors(deadWarriors);
            currentWheat = 0;
        }
        else
        {
            currentWheat -= amount;
        }
        UpdateUI();
    }

    // Атака врагов
    private void Attack(int enemyCount)
    {
        int casualties = Mathf.Min(currentWarriors, enemyCount);
        KillWarriors(casualties);
        enemyCount -= casualties;

        if (enemyCount > 0)
        {
            int peasantCasualties = enemyCount * 3;
            KillPeasants(peasantCasualties);
        }

        UpdateUI();
        CheckDefeatConditions();
    }

        // Убить определенное количество воинов
    private void KillWarriors(int count)
    {
        currentWarriors -= count;
        if (currentWarriors < 0) currentWarriors = 0;
        UpdateUI();
    }

    // Убить определенное количество крестьян
    private void KillPeasants(int count)
    {
        currentPeasants -= count;
        if (currentPeasants < 0) currentPeasants = 0;
        UpdateUI();
    }

    // Проверка условий победы
    private void CheckVictoryConditions()
    {
        if (currentWheat >= victoryWheatThreshold && currentPeasants + currentWarriors >= victoryPopulationThreshold)
        {
            StopAllCoroutines(); // Останавливаем все корутины
            winPopup.SetActive(true);
        }
    }

    // Проверка условий поражения
    private void CheckDefeatConditions()
    {
        if (currentPeasants == 0 && currentWarriors == 0)
        {
            StopAllCoroutines(); // Останавливаем все корутины
            losePopup.SetActive(true);
        }
    }

    // Обработчик нажатия кнопки найма крестьянина
    public void OnHirePeasantClick()
    {
        if (!isPeasantHiring && currentWheat >= peasantHireCost)
        {
            currentWheat -= peasantHireCost;
            peasantHiringRoutine = StartCoroutine(HirePeasant());
            UpdateUI();
        }
    }

    // Обработчик нажатия кнопки найма воина
    public void OnHireWarriorClick()
    {
        if (!isWarriorHiring && currentWheat >= warriorHireCost)
        {
            currentWheat -= warriorHireCost;
            warriorHiringRoutine = StartCoroutine(HireWarrior());
            UpdateUI();
        }
    }

    // Перезапустить игру
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

