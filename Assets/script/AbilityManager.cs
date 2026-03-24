using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("참조")]
    public PlayerMovement playerMovement;
    public CharacterController controller;

    [Header("대쉬 설정")]
    public float dashForce = 100f;
    public float dashDuration = 0.2f;

    [System.Serializable]
    public class Ability
    {
        public string name;
        public KeyCode key;
        public float cooldownTime;
        public SpriteCooldown ui;
    }

    [Header("기술 리스트 (0: Scan, 1: Dash 순서 권장)")]
    public List<Ability> abilities = new List<Ability>();

    void Update()
    {
        if (playerMovement != null && playerMovement.isUIOpen) return;

        foreach (Ability ability in abilities)
        {
            if (Input.GetKeyDown(ability.key))
            {
                if (ability.ui != null && !ability.ui.IsOnCooldown())
                {
                    if (ability.name == "Dash" && (playerMovement == null || !playerMovement.hasFins)) continue;
                    ExecuteAbility(ability);
                }
            }
        }
    }

    private void ExecuteAbility(Ability ability)
    {
        ability.ui.StartCooldown(ability.cooldownTime);

        switch (ability.name)
        {
            case "Scan":
                DoScan();
                break;
            case "Dash":
                StartCoroutine(DashRoutine());
                break;
        }
    }

    private void DoScan()
    {
        DirtPainter[] painters = Object.FindObjectsByType<DirtPainter>(FindObjectsSortMode.None);
        foreach (var p in painters) p.RevealDirt(1f);

        SubmarinePart[] submarineParts = Object.FindObjectsByType<SubmarinePart>(FindObjectsSortMode.None);
        foreach (var part in submarineParts) part.RevealDirt(1f);

        Debug.Log("스캔 완료");
    }

    IEnumerator DashRoutine()
    {
        float startTime = Time.time;
        Vector3 dashDir = Camera.main.transform.forward;

        Debug.Log("대쉬 시작! 방향: " + dashDir);

        while (Time.time < startTime + dashDuration)
        {
            controller.Move(dashDir * dashForce * Time.deltaTime);
            yield return null;
        }

        Debug.Log("대쉬 끝");
    }
}
