using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using Photon.Pun;

public class BattleUI : MonoBehaviour
{
    [SerializeField] TeamUI[] teamUIs;
    [SerializeField] RectTransform minimapRedShip, minimapBlueShip;
    [SerializeField] TMP_Text redScore, blueScore;

    GameHandler gameHandler;

    private void Start()
    {
        gameHandler = ServiceLocator.GetGameHandler();
        gameHandler.AllReady += OnAllReady;
        gameHandler.ScoreChanged += OnScoreChanged;
        gameHandler.OnSpaceshipSpawned += OnSpaceshipSpawned;
    }

    private void OnSpaceshipSpawned(Spaceship obj)
    {
        obj.HealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        if (gameHandler != null)
        {
            gameHandler.AllReady -= OnAllReady;
            gameHandler.ScoreChanged -= OnScoreChanged;
        }
    }

    private void OnScoreChanged()
    {
        redScore.text = gameHandler.GetScoreOf(Team.Red).ToString();
        blueScore.text = gameHandler.GetScoreOf(Team.Blue).ToString();
    }

    private void OnAllReady()
    {
        RefreshUI();
    }

    private void Update()
    {
        var localTeam = PhotonNetwork.LocalPlayer.GetTeam();

        if (localTeam == Team.None)
            return;

        if (localTeam == Team.Red)
        {
            minimapRedShip.anchoredPosition = Vector2.zero;
            var myShip = ServiceLocator.GetLocationTransform(Team.Red, Location.Cockpit);
            var enemyShip = ServiceLocator.GetLocationTransform(Team.Blue, Location.Cockpit);

            if (myShip != null && enemyShip != null)
                minimapBlueShip.anchoredPosition = (enemyShip.position - myShip.position) * 0.5f;
        }
        else
        {
            minimapBlueShip.anchoredPosition = Vector2.zero;
            var myShip = ServiceLocator.GetLocationTransform(Team.Blue, Location.Cockpit);
            var enemyShip = ServiceLocator.GetLocationTransform(Team.Red, Location.Cockpit);

            if (myShip != null && enemyShip != null)
                minimapRedShip.anchoredPosition = (enemyShip.position - myShip.position) * 0.5f;
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
