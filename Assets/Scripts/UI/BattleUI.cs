using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class BattleUI : MonoBehaviour
{
    [SerializeField] TeamUI[] teamUIs;

    private void Start()
    {
        ServiceLocator.GetGameHandler().AllReady += OnAllReady;
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsValid())
            ServiceLocator.GetGameHandler().AllReady -= OnAllReady;
    }

    private void OnAllReady()
    {
        RefreshUI();

        foreach (var teamUI in teamUIs)
        {
            var spaceship = ServiceLocator.GetSpaceship(teamUI.Team);
            spaceship.HealthChanged += OnHealthChanged;
        }
    }

    private void OnHealthChanged(Spaceship ship)
    {
        var ui = teamUIs.FirstOrDefault((x) => x.Team == ship.Team);
        if (ui == null)
        {
            Debug.LogWarning("No team UI specified for " + ship.Team);
        }
        else
        {
            ui.Healthbar.maxValue = ship.MaxHealth;
            ui.Healthbar.value = ship.Health;
        }
    }

    private void RefreshUI()
    {
        foreach (var teamUI in teamUIs)
        {
            var pilot = ServiceLocator.GetFirstPlayerMatching(teamUI.Team, PlayerRole.Pilot);
            teamUI.PilotText.text = teamUI.Team + " Pilot: " + (pilot != null ? pilot.NickName : "Empty");

            var gunner = ServiceLocator.GetFirstPlayerMatching(teamUI.Team, PlayerRole.Gunner);
            teamUI.GunnerText.text = teamUI.Team + " Gunner: " + (gunner != null ? gunner.NickName : "Empty");
        }
    }

    [System.Serializable]
    public class TeamUI
    {
        public Team Team;
        public Slider Healthbar;
        public TMP_Text PilotText;
        public TMP_Text GunnerText;
    }
}
