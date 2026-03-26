using UnityEngine;
using UnityEngine.UI; // 피통(이미지) 조절을 위해 꼭 필요해!

public class Barnacle : MonoBehaviour
{
    [Header("설정")]
    public int hp = 2;              // 처음 피 (2번 때리기)
    public int scoreValue = 15;     // 주울 때 줄 돈 (15원)
    public GameObject collectEffect; // 다 깨지면 나올 반짝이
    public Image hpBar;             // 아까 만든 '빨간 피통 이미지'를 여기 드래그해!

    [Header("사운드")]
    public AudioClip hitSound;
    public AudioClip collectSound;
    [Range(0f, 1f)] public float hitVolume = 1f;
    [Range(0f, 1f)] public float collectVolume = 1f;

    private int maxHp;              // 처음 피가 얼마였는지 기억용
    private bool _canCollect = false;

    void Start()
    {
        maxHp = hp; // 처음에 설정한 피(2)를 기억해둬

        // 시작할 때는 반짝이를 꺼둘게
        if (collectEffect != null) collectEffect.SetActive(false);

        // 피통 UI 업데이트 (처음엔 꽉 차있게)
        UpdateHPUI();
    }

    // 낫으로 때릴 때마다 실행되는 부분
    public void TakeDamage(int damage)
    {
        if (_canCollect) return; // 이미 다 깨졌으면 무시

        hp -= damage;   // 피 깎기
        PlayClip(hitSound, hitVolume);
        UpdateHPUI();   // 피통 줄어드는 모습 보여주기

        if (hp <= 0)
        {
            SetCollectable();
        }
    }

    // 피통 이미지를 쓱쓱 줄이는 기능
    private void UpdateHPUI()
    {
        if (hpBar != null)
        {
            // (남은피 / 처음피) 계산해서 이미지 길이를 조절해 (0.5, 0 이런 식으로)
            hpBar.fillAmount = (float)hp / maxHp;
        }
    }

    // 다 깨졌을 때 (아이템 상태로 변신)
    private void SetCollectable()
    {
        _canCollect = true;

        // 1. 피통 숨기기 (다 깨졌으니까 필요 없지?)
        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(false);

        // 2. 반짝이는 이펙트 켜기
        if (collectEffect != null) collectEffect.SetActive(true);

        // 3. 따개비 색깔을 회색으로 바꾸기
        if (GetComponent<Renderer>() != null)
            GetComponent<Renderer>().material.color = Color.gray;

        Debug.Log("따개비 수거 가능 상태!");
    }

    // E키로 주울 때 실행되는 부분
    public void Collect()
    {
        if (!_canCollect) return;
        PlayClip(collectSound, collectVolume);

        // 돈 주기
        if (CoinManager.instance != null) CoinManager.instance.AddCoins(scoreValue);

        // 왼쪽 UI 미션 개수 줄이기
        if (MissionManager.Instance != null) MissionManager.Instance.OnTrashPickedUp();

        // 따개비 삭제
        Destroy(gameObject);
    }

    public bool CanCollect() => _canCollect;

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameObject audioObject = new GameObject("BarnacleSound");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.Play();

        Destroy(audioObject, clip.length + 0.1f);
    }
}
