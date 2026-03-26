using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Component References")]
    public CharacterController controller;
    public Transform playerBody;
    private Camera mainCam;
    private Vector3 defaultCameraLocalPos;

    [Header("Start Flow")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private Toggle tutorialMapToggle;
    [SerializeField] private Toggle submarineMapToggle;
    [SerializeField] private TextMeshProUGUI mapSelectionWarningText;
    private bool hasGameStarted;
    private bool isWaitingForInitialMissionSelection;

    [Header("Movement Settings")]
    public float speed = 55f;
    public float swimSpeed = 40f;
    public float sinkSpeed = 30f;
    public float minHeight = 1.5f;

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchCameraOffset = 0.6f;
    public float crouchTransitionSpeed = 8f;
    private bool isCrouching;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    private float xRotation;

    [Header("Oxygen System")]
    public float maxOxygen = 100f;
    public float currentOxygen;
    public Image oxygenBar;
    [HideInInspector] public bool isInBase;

    [Header("Oxygen Warning")]
    [SerializeField] private Image oxygenWarningOverlay;
    [Range(0f, 1f)] public float oxygenWarningThreshold = 0.1f;
    [Range(0f, 1f)] public float maxWarningOverlayAlpha = 0.75f;
    public int oxygenDepletionPenalty = 500;

    [Header("Death Handling")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Transform respawnPoint;
    private Vector3 startPosition;
    private bool isDead;

    [Header("UI System")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject upgradeScreen;
    public bool isUIOpen => isUpgradeOpen || isMissionMenuOpen || isHelpOpen;
    private bool isUpgradeOpen;
    private bool isMissionMenuOpen;
    private bool isHelpOpen;
    private bool isCursorUnlockedByEsc;

    [Header("Status UI")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI finStatusText;
    [SerializeField] private TextMeshProUGUI finLevelText;
    [SerializeField] private TextMeshProUGUI oxygenLevelText;
    [SerializeField] private TextMeshProUGUI oxygenRankText;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button buyFinBtn;
    [SerializeField] private TextMeshProUGUI buyFinBtnText;
    [SerializeField] private Button upFinBtn;
    [SerializeField] private TextMeshProUGUI upFinBtnText;
    [SerializeField] private Button upOxyCapBtn;
    [SerializeField] private TextMeshProUGUI upOxyCapBtnText;
    [SerializeField] private Button upOxyEffBtn;
    [SerializeField] private TextMeshProUGUI upOxyEffBtnText;

    [Header("Player Stats")]
    public bool hasFins;
    public int finLevel = 1;
    public int oxygenCapLevel = 1;
    public int oxygenEffLevel = 1;
    public int maxUpgradeLevel = 20;

    private int currentBuyFinCost = 100;
    private int currentUpFinCost = 50;
    private int currentUpOxyCapCost = 50;
    private int currentUpOxyEffCost = 80;

    [Header("Cleaning System")]
    public float cleanDistance = 50f;
    public enum WaterMode { Strong, Mid, Weak }
    public WaterMode currentMode = WaterMode.Mid;
    public float[] brushSizes = { 0.15f, 0.07f, 0.02f };
    public float[] cleanSpeeds = { 1.5f, 2.5f, 5.5f };
    public TextMeshProUGUI nozzleStatusText;

    [Header("Water VFX")]
    public List<GameObject> waterParticleObjects = new List<GameObject>();

    [Header("Audio")]
    public AudioSource waterAudioSource;
    public AudioSource uiAudioSource;
    public AudioClip waterClip;
    public AudioClip uiOpenClip;
    public AudioClip uiCloseClip;

    [Header("Sickle Settings")]
    public Transform sickleVisual;
    public float attackRange = 3.0f;
    public float attackCooldown = 0.01f;
    public float attackSwingInDuration = 0.025f;
    public float attackSwingOutDuration = 0.04f;
    private bool isAttacking;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null) defaultCameraLocalPos = mainCam.transform.localPosition;

        if (oxygenWarningOverlay == null)
        {
            GameObject overlayObject = GameObject.Find("BlindOverlay");
            if (overlayObject != null) oxygenWarningOverlay = overlayObject.GetComponent<Image>();
        }

        if (deathPanel == null)
        {
            GameObject foundDeathPanel = GameObject.Find("DeathPanel");
            if (foundDeathPanel != null) deathPanel = foundDeathPanel;
        }

        if (startPanel == null)
        {
            GameObject foundStartPanel = GameObject.Find("StartPanel");
            if (foundStartPanel != null) startPanel = foundStartPanel;
        }

        if (helpPanel == null)
        {
            GameObject foundHelpPanel = GameObject.Find("HelpPanel");
            if (foundHelpPanel != null) helpPanel = foundHelpPanel;
        }

        if (mapSelectionPanel == null)
        {
            GameObject foundMapSelectionPanel = GameObject.Find("MapSelectionPanel");
            if (foundMapSelectionPanel != null) mapSelectionPanel = foundMapSelectionPanel;
        }

        startPosition = transform.position;
        currentOxygen = maxOxygen;

        foreach (GameObject obj in waterParticleObjects)
        {
            if (obj != null) obj.SetActive(false);
        }

        if (upgradeScreen != null) upgradeScreen.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);
        if (mapSelectionPanel != null) mapSelectionPanel.SetActive(false);

        UpdateNozzleUI();
        UpdateOxygenWarningOverlay();
        SetStartState();
    }

    void Update()
    {
        if (!hasGameStarted)
        {
            StopAllParticles();
            if (waterAudioSource != null && waterAudioSource.isPlaying) waterAudioSource.Stop();
            return;
        }

        HandleEscapeCursorToggle();
        HandleMouseRelock();

        if (Input.GetKeyDown(KeyCode.N)) ToggleUpgrade();
        if (Input.GetKeyDown(KeyCode.M)) ToggleMissionMenu();
        if (Input.GetKeyDown(KeyCode.H)) ToggleHelpPanel();

        isMissionMenuOpen = MissionManager.Instance != null &&
                            MissionManager.Instance.mMenuPanel != null &&
                            MissionManager.Instance.mMenuPanel.activeSelf;

        if (isWaitingForInitialMissionSelection)
        {
            if (MissionManager.Instance != null && MissionManager.Instance.HasAnyAcceptedMission)
            {
                isWaitingForInitialMissionSelection = false;
            }
            else
            {
                ForceOpenInitialMissionMenu();
                UpdateCrouchCamera();
                StopAllParticles();
                if (waterAudioSource != null && waterAudioSource.isPlaying) waterAudioSource.Stop();
                return;
            }
        }

        if (isDead)
        {
            UpdateOxygenWarningOverlay();
            StopAllParticles();
            if (waterAudioSource != null && waterAudioSource.isPlaying) waterAudioSource.Stop();
            return;
        }

        if (isUIOpen || isCursorUnlockedByEsc)
        {
            UpdateCrouchCamera();
            StopAllParticles();
            if (waterAudioSource != null && waterAudioSource.isPlaying) waterAudioSource.Stop();
            return;
        }

        HandleRotation();
        HandleMovement();
        UpdateCrouchCamera();
        HandleOxygen();
        HandleInputs();
        HandleWaterEffect();

        if (waterAudioSource != null && waterAudioSource.isPlaying && waterAudioSource.time >= 3.0f)
        {
            waterAudioSource.time = 0.0f;
        }
    }

    private void SetStartState()
    {
        hasGameStarted = false;
        isWaitingForInitialMissionSelection = false;
        if (startPanel != null) startPanel.SetActive(true);
        if (mapSelectionPanel != null) mapSelectionPanel.SetActive(false);
        if (mapSelectionWarningText != null) mapSelectionWarningText.gameObject.SetActive(false);
        SetCursor(true);
    }

    public void StartGame()
    {
        if (hasGameStarted) return;

        BeginGameplay();
        isWaitingForInitialMissionSelection = true;
        ForceOpenInitialMissionMenu();
    }

    public void ConfirmMapSelectionAndStart()
    {
        bool tutorialSelected = tutorialMapToggle == null || tutorialMapToggle.isOn;
        bool submarineSelected = submarineMapToggle == null || submarineMapToggle.isOn;

        if (!tutorialSelected && !submarineSelected)
        {
            if (mapSelectionWarningText != null)
            {
                mapSelectionWarningText.text = "맵을 하나 이상 선택해야 합니다.";
                mapSelectionWarningText.gameObject.SetActive(true);
            }
            return;
        }

        ApplySelectedMaps(tutorialSelected, submarineSelected);

        if (mapSelectionWarningText != null) mapSelectionWarningText.gameObject.SetActive(false);
        if (mapSelectionPanel != null) mapSelectionPanel.SetActive(false);

        BeginGameplay();
    }

    public void BackToStartPanel()
    {
        if (mapSelectionPanel != null) mapSelectionPanel.SetActive(false);
        if (startPanel != null) startPanel.SetActive(true);
        if (mapSelectionWarningText != null) mapSelectionWarningText.gameObject.SetActive(false);
        SetCursor(true);
    }

    private void BeginGameplay()
    {
        hasGameStarted = true;
        isCursorUnlockedByEsc = false;
        if (startPanel != null) startPanel.SetActive(false);
        if (mapSelectionPanel != null) mapSelectionPanel.SetActive(false);
        SetCursor(false);
    }

    private void ApplySelectedMaps(bool tutorialSelected, bool submarineSelected)
    {
        if (MissionManager.Instance == null) return;

        if (MissionManager.Instance.tutorialJobCard != null)
            MissionManager.Instance.tutorialJobCard.SetActive(tutorialSelected);

        if (MissionManager.Instance.submarineJobCard != null)
            MissionManager.Instance.submarineJobCard.SetActive(submarineSelected);

        if (MissionManager.Instance.tutorialMapGroup != null)
            MissionManager.Instance.tutorialMapGroup.SetActive(false);

        if (MissionManager.Instance.submarineMapGroup != null)
            MissionManager.Instance.submarineMapGroup.SetActive(false);
    }

    private void ForceOpenInitialMissionMenu()
    {
        if (MissionManager.Instance == null || MissionManager.Instance.mMenuPanel == null) return;

        if (isUpgradeOpen) ToggleUpgrade();
        if (isHelpOpen) SetHelpPanel(false);

        MissionManager.Instance.mMenuPanel.SetActive(true);
        MissionManager.Instance.ShowAvailableJobs();
        isMissionMenuOpen = true;
        isCursorUnlockedByEsc = false;
        SetCursor(true);
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouching = !isCrouching;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentMode = (WaterMode)(((int)currentMode + 1) % 3);
            UpdateNozzleUI();
        }

        if (Input.GetMouseButtonDown(0) && !isUIOpen)
        {
            if (!isAttacking) StartCoroutine(SwingSickle());
        }

        if (Input.GetMouseButtonDown(1) && !isUIOpen)
        {
            if (waterClip != null && waterAudioSource != null)
            {
                waterAudioSource.clip = waterClip;
                waterAudioSource.Play();
            }
        }

        if (Input.GetMouseButtonUp(1) || isUIOpen)
        {
            if (waterAudioSource != null && waterAudioSource.isPlaying) waterAudioSource.Stop();
        }

        if (Input.GetKeyDown(KeyCode.E)) TryPickupItems();

        if (Input.GetMouseButton(1)) HandleCleaning();
    }

    private IEnumerator SwingSickle()
    {
        isAttacking = true;

        Vector3 rayOrigin = mainCam.transform.position;
        Vector3 rayDirection = mainCam.transform.forward;
        int layerMask = ~(1 << LayerMask.NameToLayer("Player"));
        float maxAttackDist = 50.0f;

        Debug.DrawRay(rayOrigin, rayDirection * maxAttackDist, Color.red, 1.0f);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxAttackDist, layerMask))
        {
            Debug.Log($"<color=yellow>낫에 맞은 물체: {hit.collider.name}</color>");

            Barnacle barnacle = hit.collider.GetComponent<Barnacle>();
            if (barnacle == null) barnacle = hit.collider.GetComponentInParent<Barnacle>();

            if (barnacle != null)
            {
                barnacle.TakeDamage(1);
            }
        }

        if (sickleVisual != null)
        {
            Quaternion startRot = sickleVisual.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(60, 0, 0);

            float elapsed = 0f;
            while (elapsed < attackSwingInDuration)
            {
                sickleVisual.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / attackSwingInDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < attackSwingOutDuration)
            {
                sickleVisual.localRotation = Quaternion.Slerp(endRot, startRot, elapsed / attackSwingOutDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            sickleVisual.localRotation = startRot;
        }

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void TryPickupItems()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 50.0f))
        {
            Barnacle barnacle = hit.collider.GetComponent<Barnacle>();
            if (barnacle != null)
            {
                if (barnacle.CanCollect()) barnacle.Collect();
                else Debug.Log("아직 단단합니다!");
                return;
            }
        }

        if (Physics.SphereCast(ray, 1.5f, out RaycastHit trashHit, 40f, 1 << LayerMask.NameToLayer("Trash")))
        {
            Trash trash = trashHit.collider.GetComponent<Trash>() ?? trashHit.collider.GetComponentInParent<Trash>();
            if (trash != null)
            {
                if (CoinManager.instance != null) CoinManager.instance.AddCoins(trash.scoreValue);
                if (MissionManager.Instance != null) MissionManager.Instance.OnTrashPickedUp();
                Destroy(trash.gameObject);
            }
        }
    }

    private void HandleWaterEffect()
    {
        if (waterParticleObjects.Count == 0) return;

        bool isFiring = Input.GetMouseButton(1) && !isUIOpen;

        if (isFiring)
        {
            int totalCount = waterParticleObjects.Count;
            int activeCount = currentMode == WaterMode.Strong ? totalCount :
                currentMode == WaterMode.Mid ? Mathf.Max(1, totalCount / 2 + 1) : 1;

            for (int i = 0; i < totalCount; i++)
            {
                if (waterParticleObjects[i] == null) continue;
                waterParticleObjects[i].SetActive(i < activeCount);
            }
        }
        else
        {
            StopAllParticles();
        }
    }

    private void StopAllParticles()
    {
        foreach (GameObject obj in waterParticleObjects)
        {
            if (obj != null && obj.activeSelf) obj.SetActive(false);
        }
    }

    private void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        float currentMoveSpeed = speed;

        if (hasFins && Input.GetKey(KeyCode.LeftShift)) currentMoveSpeed = speed * 1.4f;

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.magnitude > 0.1f) controller.Move(move * currentMoveSpeed * Time.deltaTime);

        float finalSwimSpeed = hasFins && Input.GetKey(KeyCode.LeftShift) ? swimSpeed * 1.4f : swimSpeed;
        if (Input.GetKey(KeyCode.Space))
        {
            controller.Move(Vector3.up * finalSwimSpeed * Time.deltaTime);
        }
        else if (transform.position.y > minHeight)
        {
            controller.Move(Vector3.down * sinkSpeed * Time.deltaTime);
        }
    }

    private void HandleCleaning()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, cleanDistance, 1 << LayerMask.NameToLayer("Dirt")))
        {
            DirtPainter painter = hit.collider.GetComponent<DirtPainter>();
            if (painter != null) painter.Paint(hit.textureCoord, brushSizes[(int)currentMode], cleanSpeeds[(int)currentMode]);

            float worldRadius = currentMode == WaterMode.Strong ? 3.0f : currentMode == WaterMode.Mid ? 1.5f : 0.5f;
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, worldRadius, 1 << LayerMask.NameToLayer("Dirt"));

            foreach (Collider col in nearbyColliders)
            {
                SubmarinePart part = col.GetComponent<SubmarinePart>();
                if (part != null) part.CleanByWorldPos(hit.point, worldRadius, cleanSpeeds[(int)currentMode]);
            }
        }
    }

    private void ToggleMissionMenu()
    {
        if (MissionManager.Instance == null || MissionManager.Instance.mMenuPanel == null) return;
        if (isWaitingForInitialMissionSelection && !MissionManager.Instance.HasAnyAcceptedMission)
        {
            ForceOpenInitialMissionMenu();
            return;
        }

        GameObject menu = MissionManager.Instance.mMenuPanel;
        bool isActive = !menu.activeSelf;
        isCursorUnlockedByEsc = false;
        if (isHelpOpen) SetHelpPanel(false);
        menu.SetActive(isActive);

        if (isActive)
        {
            if (isUpgradeOpen) ToggleUpgrade();
            MissionManager.Instance.ShowAvailableJobs();
            SetCursor(true);
            if (uiAudioSource != null && uiOpenClip != null) uiAudioSource.PlayOneShot(uiOpenClip);
        }
        else
        {
            SetCursor(false);
            if (uiAudioSource != null && uiCloseClip != null) uiAudioSource.PlayOneShot(uiCloseClip);
        }
    }

    private void ToggleUpgrade()
    {
        isUpgradeOpen = !isUpgradeOpen;
        isCursorUnlockedByEsc = false;
        if (isUpgradeOpen && isHelpOpen) SetHelpPanel(false);

        if (upgradeScreen != null) upgradeScreen.SetActive(isUpgradeOpen);

        if (isUpgradeOpen)
        {
            UpdateStatusUI();
            if (MissionManager.Instance != null && MissionManager.Instance.mMenuPanel != null)
                MissionManager.Instance.mMenuPanel.SetActive(false);

            SetCursor(true);
            if (uiAudioSource != null && uiOpenClip != null) uiAudioSource.PlayOneShot(uiOpenClip);
        }
        else
        {
            SetCursor(false);
            if (uiAudioSource != null && uiCloseClip != null) uiAudioSource.PlayOneShot(uiCloseClip);
        }
    }

    private void SetCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    private void UpdateNozzleUI()
    {
        if (nozzleStatusText != null)
        {
            nozzleStatusText.text = "노즐 : " + (currentMode == WaterMode.Strong ? "광범위" :
                                   currentMode == WaterMode.Mid ? "일반" : "집중");
        }
    }

    public void BuyFins()
    {
        if (CoinManager.instance == null) return;
        if (!hasFins && CoinManager.instance.currentCoins >= currentBuyFinCost)
        {
            CoinManager.instance.SubtractCoins(currentBuyFinCost);
            hasFins = true;
            UpdateStatusUI();
        }
    }

    public void UpgradeFinLevel()
    {
        if (CoinManager.instance == null) return;
        if (hasFins && finLevel < maxUpgradeLevel && CoinManager.instance.currentCoins >= currentUpFinCost)
        {
            CoinManager.instance.SubtractCoins(currentUpFinCost);
            finLevel++;
            speed += 10f;
            swimSpeed += 7f;
            currentUpFinCost += 60;
            UpdateStatusUI();
        }
    }

    public void UpgradeOxygenCapacity()
    {
        if (CoinManager.instance == null) return;
        if (oxygenCapLevel < maxUpgradeLevel && CoinManager.instance.currentCoins >= currentUpOxyCapCost)
        {
            CoinManager.instance.SubtractCoins(currentUpOxyCapCost);
            oxygenCapLevel++;
            maxOxygen += 25f;
            currentOxygen = maxOxygen;
            currentUpOxyCapCost += 40;
            UpdateStatusUI();
        }
    }

    public void UpgradeOxygenEfficiency()
    {
        if (CoinManager.instance == null) return;
        if (oxygenEffLevel < maxUpgradeLevel && CoinManager.instance.currentCoins >= currentUpOxyEffCost)
        {
            CoinManager.instance.SubtractCoins(currentUpOxyEffCost);
            oxygenEffLevel++;
            currentUpOxyEffCost += 70;
            UpdateStatusUI();
        }
    }

    public void UpdateStatusUI()
    {
        if (CoinManager.instance != null && coinText != null) coinText.text = $"현재 보유 코인: {CoinManager.instance.currentCoins}G";
        if (finStatusText != null) finStatusText.text = hasFins ? "이동 장비: 오리발 (달리기/대쉬 가능)" : "이동 장비: 맨발";
        if (finLevelText != null) finLevelText.text = hasFins ? $"오리발 레벨: Lv.{finLevel}" : "오리발 레벨: 미획득";
        if (oxygenLevelText != null) oxygenLevelText.text = $"산소통 레벨: Lv.{oxygenCapLevel} (최대 {maxOxygen:F0})";
        if (oxygenRankText != null) oxygenRankText.text = $"산소통 등급: {GetOxygenRankName(oxygenEffLevel)}";

        if (CoinManager.instance == null) return;

        if (buyFinBtn != null) buyFinBtn.interactable = !hasFins && CoinManager.instance.currentCoins >= currentBuyFinCost;
        if (buyFinBtnText != null) buyFinBtnText.text = hasFins ? "획득 완료" : $"구매 ({currentBuyFinCost}G)";

        if (upFinBtn != null) upFinBtn.interactable = hasFins && finLevel < maxUpgradeLevel && CoinManager.instance.currentCoins >= currentUpFinCost;
        if (upFinBtnText != null) upFinBtnText.text = !hasFins ? "오리발 필요" : finLevel >= maxUpgradeLevel ? "최대" : $"강화 ({currentUpFinCost}G)";

        if (upOxyCapBtn != null) upOxyCapBtn.interactable = oxygenCapLevel < maxUpgradeLevel && CoinManager.instance.currentCoins >= currentUpOxyCapCost;
        if (upOxyCapBtnText != null) upOxyCapBtnText.text = oxygenCapLevel >= maxUpgradeLevel ? "최대" : $"확장 ({currentUpOxyCapCost}G)";

        if (oxygenEffLevel >= maxUpgradeLevel)
        {
            if (upOxyEffBtn != null) upOxyEffBtn.interactable = false;
            if (upOxyEffBtnText != null) upOxyEffBtnText.text = "최대";
        }
        else
        {
            if (upOxyEffBtn != null) upOxyEffBtn.interactable = CoinManager.instance.currentCoins >= currentUpOxyEffCost;
            if (upOxyEffBtnText != null) upOxyEffBtnText.text = $"강화 ({currentUpOxyEffCost}G)";
        }
    }

    private string GetOxygenRankName(int level)
    {
        switch (level)
        {
            case 1: return "일반";
            case 2: return "강화";
            case 3: return "전문가";
            default: return "심해용";
        }
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (mainCam != null) mainCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null) playerBody.Rotate(Vector3.up * mouseX);
    }

    private void UpdateCrouchCamera()
    {
        if (mainCam == null) return;

        Vector3 targetLocalPos = defaultCameraLocalPos;
        if (isCrouching) targetLocalPos.y -= crouchCameraOffset;

        mainCam.transform.localPosition = Vector3.Lerp(
            mainCam.transform.localPosition,
            targetLocalPos,
            Time.deltaTime * crouchTransitionSpeed);
    }

    private void HandleEscapeCursorToggle()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || isDead || !hasGameStarted) return;

        if (isHelpOpen)
        {
            SetHelpPanel(false);
            return;
        }

        if (isUpgradeOpen)
        {
            ToggleUpgrade();
            return;
        }

        if (MissionManager.Instance != null &&
            MissionManager.Instance.mMenuPanel != null &&
            MissionManager.Instance.mMenuPanel.activeSelf)
        {
            if (isWaitingForInitialMissionSelection && !MissionManager.Instance.HasAnyAcceptedMission)
            {
                ForceOpenInitialMissionMenu();
                return;
            }
            ToggleMissionMenu();
            return;
        }

        isCursorUnlockedByEsc = !isCursorUnlockedByEsc;
        SetCursor(isCursorUnlockedByEsc);
    }

    private void HandleMouseRelock()
    {
        if (!isCursorUnlockedByEsc || isDead || !hasGameStarted) return;
        if (isUpgradeOpen || isMissionMenuOpen) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            isCursorUnlockedByEsc = false;
            SetCursor(false);
        }
    }

    public void ToggleHelpPanel()
    {
        SetHelpPanel(!isHelpOpen);
    }

    public void OpenHelpPanel()
    {
        SetHelpPanel(true);
    }

    public void CloseHelpPanel()
    {
        SetHelpPanel(false);
    }

    private void SetHelpPanel(bool isOpen)
    {
        isHelpOpen = isOpen;
        isCursorUnlockedByEsc = false;

        if (helpPanel != null) helpPanel.SetActive(isOpen);

        if (isOpen)
        {
            if (isUpgradeOpen) ToggleUpgrade();
            if (MissionManager.Instance != null && MissionManager.Instance.mMenuPanel != null && MissionManager.Instance.mMenuPanel.activeSelf)
            {
                MissionManager.Instance.mMenuPanel.SetActive(false);
                isMissionMenuOpen = false;
            }
            SetCursor(true);
        }
        else if (!isUpgradeOpen && !isMissionMenuOpen && !isDead)
        {
            SetCursor(false);
        }
    }

    private void HandleOxygen()
    {
        if (isInBase)
        {
            currentOxygen += Time.deltaTime * 20f;
        }
        else
        {
            float rate = 0.5f * (1.1f - (oxygenEffLevel * 0.1f));
            currentOxygen -= Time.deltaTime * rate;
        }

        currentOxygen = Mathf.Clamp(currentOxygen, 0, maxOxygen);

        if (oxygenBar != null) oxygenBar.fillAmount = currentOxygen / maxOxygen;
        UpdateOxygenWarningOverlay();

        if (currentOxygen <= 0f && !isDead)
        {
            HandleDeathByOxygen();
        }
    }

    private void UpdateOxygenWarningOverlay()
    {
        if (oxygenWarningOverlay == null || maxOxygen <= 0f) return;

        float oxygenRatio = currentOxygen / maxOxygen;
        float alpha = 0f;

        if (oxygenRatio < oxygenWarningThreshold)
        {
            float t = 1f - Mathf.Clamp01(oxygenRatio / oxygenWarningThreshold);
            alpha = t * maxWarningOverlayAlpha;
        }

        Color color = oxygenWarningOverlay.color;
        color.a = alpha;
        oxygenWarningOverlay.color = color;
    }

    private void HandleDeathByOxygen()
    {
        isDead = true;
        currentOxygen = 0f;
        isCursorUnlockedByEsc = false;
        StopAllParticles();

        if (waterAudioSource != null && waterAudioSource.isPlaying) waterAudioSource.Stop();
        if (deathPanel != null) deathPanel.SetActive(true);

        SetCursor(true);
    }

    public void ReturnToBaseAfterDeath()
    {
        if (!isDead) return;

        if (CoinManager.instance != null)
        {
            CoinManager.instance.SubtractCoins(oxygenDepletionPenalty);
        }

        Transform targetPoint = respawnPoint;
        if (controller != null) controller.enabled = false;

        if (targetPoint != null)
        {
            transform.position = targetPoint.position;
            if (playerBody != null) playerBody.rotation = targetPoint.rotation;
        }
        else
        {
            transform.position = startPosition;
        }

        if (controller != null) controller.enabled = true;

        currentOxygen = maxOxygen;
        isInBase = true;
        isDead = false;
        isCrouching = false;

        if (mainCam != null) mainCam.transform.localPosition = defaultCameraLocalPos;
        if (oxygenBar != null) oxygenBar.fillAmount = 1f;

        UpdateOxygenWarningOverlay();

        if (deathPanel != null) deathPanel.SetActive(false);
        SetCursor(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Base")) isInBase = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Base")) isInBase = false;
    }
}
