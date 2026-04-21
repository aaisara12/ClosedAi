using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float ElapsedTime { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool HasCountdown { get; private set; }

    public int PistolAmmo => _pistol != null ? _pistol.CurrentAmmo : 0;
    public bool PistolLoaded => _pistol != null && _pistol.IsLoaded;
    public bool PistolEquipped => _pistol != null && _pistol.IsEquipped;

    public int EnemyCount => EnemyCounter.EnemyCount;

    public float DashCooldownRemaining => _dash != null ? _dash.CooldownRemaining : 0f;
    public float DashCooldownTotal => _dash != null ? _dash.CooldownTotal : 0f;

    public float SmokeCooldownRemaining => _smoke != null ? _smoke.CooldownRemaining : 0f;
    public float SmokeCooldownTotal => _smoke != null ? _smoke.CooldownTotal : 0f;

    private Pistol _pistol;
    private DashController _dash;
    private SmokeGadget _smoke;
    private Level _activeLevel;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        ElapsedTime += Time.deltaTime;
        TryBindComponents();
        TickCountdown();
    }

    public void StartCountdown(float timeLimit, Level level)
    {
        TimeRemaining = timeLimit;
        _activeLevel = level;
        HasCountdown = true;
    }

    private void TickCountdown()
    {
        if (!HasCountdown) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            HasCountdown = false;
            _activeLevel?.LoseLevel();
            _activeLevel = null;
        }
    }

    private void TryBindComponents()
    {
        if (_pistol == null && PlayerController.Instance != null)
            _pistol = PlayerController.Instance.GetComponent<Pistol>();

        if (_dash == null && PlayerController.Instance != null)
            _dash = PlayerController.Instance.GetComponent<DashController>();

        if (_smoke == null && PlayerController.Instance != null)
            _smoke = PlayerController.Instance.GetComponent<SmokeGadget>();
    }
}
