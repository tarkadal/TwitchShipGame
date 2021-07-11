﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

//TODO: proper namespace?
//namespace Assets.Gameplay
//{

/// <summary>
/// A class to manage player instances
/// </summary>
public class PlayerManager : ManagerBase<PlayerManager>
{
    internal BattleRoyaleState battleRoyaleState;

    internal enum BattleRoyaleState
    {
        NotTriggered,
        Mustering, // Idle.. 
        SubmissionsClosed, // Moving to start positions
        InProgress, // fighting..
        Finished // Winner chosen!
    }

    const int RANGE_OF_PLAYERS = 5, PICKUP_RANGE = 2;

    internal IEnumerable<Player> GetPlayersInBRAroundVector2(Vector3 position)
        => _players.Where(o => o.ParticipatingInBR && Vector2.SqrMagnitude(o.transform.position - position) < RANGE_OF_PLAYERS);

    internal IEnumerable<Player> GetPlayersInBRAroundVector2ForPowerUp(Vector3 position)
        => _players.Where(o => o.ParticipatingInBR && Vector2.SqrMagnitude(o.transform.position - position) < PICKUP_RANGE);


    [SerializeField]
    Transform _spawnZoneTransform = null;

    [SerializeField]
    Transform _battleZoneTranform = null;

    //TODO: multiple player prefabs?
    [SerializeField]
    Player _playerPrefab = null;

    List<Player> _players = new List<Player>();

    TarkAPI levellingAPI;
    private void Start()
    {
        levellingAPI = new TarkAPI("https://jerncoom.co.uk");
    }

    internal IEnumerator GetPlayerLevel(Player caller, Player.CrewMate cm)
    {
        yield return StartCoroutine(levellingAPI.GetPlayer(cm.Name, (req) =>
        {
            // TarkUserModel[] x = Newtonsoft.Json.JsonConvert.SerializeObject(req);

            if (req == null || req.Length <= 0)
            {
                StartCoroutine(CreatePlayerForLevelling(cm.Name));
                cm.Level = 1;
            }

            foreach(TarkUserModel tum in req)
            {
                Debug.Log(tum.name);
                Debug.Log(tum.level);

                cm.Level = tum.level;
                caller.RecalcTotalLevelAndDisplay();
            }
        }));
    }

    IEnumerator CreatePlayerForLevelling(string name)
    {
        yield return StartCoroutine(levellingAPI.AddPlayer(name, 1, (req) =>
        {
            print("CRTEATED" + req.success);
        }));
    }

    //IEnumerator Start()
    //{
    //    //var api = new TarkAPI("https://jerncoom.co.uk");

    //    //yield return StartCoroutine(api.GetAllPlayers((req) =>
    //    //{
    //    //    print(Newtonsoft.Json.JsonConvert.SerializeObject(req));
    //    //}));

    //    //yield return StartCoroutine(api.GetPlayer("clayman", (req) =>
    //    //{
    //    //    print(Newtonsoft.Json.JsonConvert.SerializeObject(req));
    //    //}));

    //    //yield return StartCoroutine(api.AddPlayer("horn coom", 1, (req) =>
    //    //{
    //    //    print(req.success);
    //    //}));

    //    //yield return StartCoroutine(api.AddPlayer("horn coom", 1, (req) =>
    //    //{
    //    //    print(req.success);
    //    //}));

    //    //yield return StartCoroutine(api.UpdatePlayer("test-123", 15, (req) =>
    //    //{
    //    //    print(req.success);
    //    //    //            print(Newtonsoft.Json.JsonConvert.SerializeObject(req));
    //    //}));

    //    //yield return StartCoroutine(api.DeletePlayer("test-123", (req) =>
    //    //{
    //    //    print(req.success);
    //    //    //            print(Newtonsoft.Json.JsonConvert.SerializeObject(req));
    //    //}));

    //    //yield return StartCoroutine(api.ResetAllPlease((req) =>
    //    //{
    //    //    print(req.downloadHandler.text);
    //    //    //            print(Newtonsoft.Json.JsonConvert.SerializeObject(req));
    //    //}));
    //}

    private void Awake()
    {
        //for editor... making sure we start off with a clean list of players
        _players = new List<Player>();
    }

    public Player GetPlayerByShipName(string shipName) =>
        _players.FirstOrDefault(o => o.GetShipName() == shipName);

    /// <summary>
    /// Gets a random spawn position relative to the _spawnZoneTransform
    /// </summary>
    /// <returns></returns>
    public Vector2 GetRandomSpawnPosition()
    {
        //read spawnzone bounds
        var scaleX = _spawnZoneTransform.transform.localScale.x;
        var scaleY = _spawnZoneTransform.transform.localScale.y;
        //rng       
        var x = Random.Range(-scaleX / 2, scaleX / 2);
        var y = Random.Range(1, 1 + (scaleY / 2));

        return _spawnZoneTransform.position + new Vector3(x, y);
    }

    public Vector2 GetRandomPositionInBattleZone()
    {
        //read spawnzone bounds
        var scaleX = _battleZoneTranform.transform.localScale.x;
        var scaleY = _battleZoneTranform.transform.localScale.y;

        //rng
        var x = Random.Range(-scaleX / 2, scaleX / 2);
        var y = Random.Range(-scaleY / 2, (scaleY / 2));

        return _battleZoneTranform.position + new Vector3(x, y);

    }


    private void FixedUpdate()
    {
        if (battleRoyaleState == BattleRoyaleState.InProgress)
        {
            if (GetBRPlayerCount() == 1)
                BRFinished();
        }
    }

    const int REWARD_FOR_WIN = 10;

    internal void BRFinished()
    {
        battleRoyaleState = BattleRoyaleState.Finished;

        Player winner = _players.First(o => o.ParticipatingInBR);

        var amt = REWARD_FOR_WIN / winner.GetNumberOfCrew();

        string CrewDisplay = "";

        int counter = 0;
        foreach(Player.CrewMate cm in winner.GetCrew())
        {
            counter++;

            CrewDisplay += cm.Role.ToString() + " " + cm.Name;

            if (counter == 3)
            {
                counter = 0;
                CrewDisplay = "\n";
            } else CrewDisplay += ", ";
        }

        UIManager.Instance.ShowWinnerPopup("Winner " + winner.GetShipName(),
            amt + " loot divided amongst " + winner.GetCrewCount() + " crewmates!", CrewDisplay
        );

        foreach (Player p in _players)
            p.stateMachine.ChangeState(new PlayerST_BackToChillingZone());

        PowerupManager.Instance.ClearAllSpawnedPrefabs();

        // NOT WORKING FOR WHATEVER REAASON!? D:
        foreach (Player.CrewMate cm in winner.GetCrew())
            levellingAPI.UpdatePlayer(cm.Name, cm.Level + amt, (cb) => { Debug.Log("Callback"); });

        battleRoyaleState = BattleRoyaleState.NotTriggered;
    }

    internal bool PlayerExistsSomewhere(string displayName)
    {
        foreach (Player p in _players)
            if (p.GetCrew().Any(o => o.Name == displayName))
                return true;

        return false;
    }

    /// <summary>
    /// Spawns a player
    /// </summary>
    /// <param name="playerName">name to display on top of the player sprite</param>
    /// <param name="position">position to spawn player at</param>
    /// <returns>instance of the player, can only create 1 player per playerName</returns>
    public Player Spawn(string shipName, string playerName)
    {
        //    //TODO: only allow 1 player instance?
        //    var existingPlayer = FindPlayerByName(playerName);
        //    if (existingPlayer != null) return null;

        //create instance of player prefab at position
        var instance = Instantiate(_playerPrefab, GetRandomSpawnPosition(), Quaternion.identity);

        //set instance player name to display
        instance.InitialisePlayer(shipName, playerName);

        //register ondestroy hook that will stop tracking the player in the _players list
        instance.onDestroy += (self) =>
        {
            _players.Remove(self);
        };

        //start tracking player instance
        _players.Add(instance);

        return instance;
    }

    [ContextMenu("Spawn player")]
    void SpawnDebug()
    {
        Player p = Spawn("HornCoom", "Player " + UnityEngine.Random.Range(0, float.MaxValue));
        for (int i = 0; i < Random.Range(0, 10); i++)
        {
            p.AddCrewmate("boof" + UnityEngine.Random.Range(0, float.MaxValue));
        }
    }

    [ContextMenu("Spawn 10 players")]
    void Spawn10Debug()
    {
        for (int i = 0; i < 10; i++)
        {
            SpawnDebug();
        }
    }


    [ContextMenu("Kill a player")]
    void KillAPlayer()
    {
        _players[0].DealDamage(_players[1], 1000);
    }



    [ContextMenu("Start battleroyale")]
    void StartBattleRoyaleDebug()
    {
        StartCoroutine(CoreTwitchLibSetup.TwitchLibCtrl.Instance.BeginBattleRoyale(null));
    }

    internal void BattleRoyaleMustering()
    {
        battleRoyaleState = BattleRoyaleState.Mustering;
    }

    internal void BattleRoyaleStarting()
    {
        battleRoyaleState = BattleRoyaleState.SubmissionsClosed;
        foreach (Player p in _players)
        {
            p.stateMachine.ChangeState(new PlayerSt_BattleRoyaleStarting());
        }
    }

    internal int GetCrewCount() => int.Parse(_players.Sum(o => o.GetCrewCount()).ToString());

    internal int GetPlayerCount() => _players.Count();

    internal int GetBRPlayerCount() => _players.Where(o=>o.ParticipatingInBR).Count();

    internal void BattleRoyaleAborted()
    {
        battleRoyaleState = BattleRoyaleState.NotTriggered;
        foreach (Player p in _players)
            p.ParticipatingInBR = false;
    }

    internal void BattleRoyaleStarted()
    {
        battleRoyaleState = BattleRoyaleState.InProgress;
        UIManager.Instance.ShowPopup("Battle has started!");

        foreach (Player p in _players)
        {
            p.ParticipatingInBR = true;
            p.stateMachine.ChangeState(new PlayerST_Fighting());
        }
    }

    //TODO: maybe store players in dictionary for faster search access?
    //public Player FindPlayerByName(string name) =>
    //     _players.FirstOrDefault(x => !string.IsNullOrEmpty(x.GetCrewmate(name)));
}
//}
