﻿using System;
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

    const int RANGE_OF_PLAYERS = 5;

    internal IEnumerable<Player> GetPlayersAroundVector2(Vector3 position) 
        => _players.Where(o => Vector2.SqrMagnitude(o.transform.position - position) < RANGE_OF_PLAYERS);

    [SerializeField]
    Transform _spawnZoneTransform = null;

    [SerializeField]
    Transform _battleZoneTranform = null;

    //TODO: multiple player prefabs?
    [SerializeField]
    Player _playerPrefab = null;

    List<Player> _players = new List<Player>();

    private void Awake()
    {
        //for editor... making sure we start off with a clean list of players
        _players = new List<Player>();
        
        /*
        for (int i = 0; i < 10; i++)
        {
            Spawn("HORN COOM", GetRandomSpawnPosition());
        }
        */
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
        var y = Random.Range(1, 1+(scaleY / 2));

        return _spawnZoneTransform.position + new Vector3(x, y);
    }

    public Vector2 GetRandomPositionInBattleZone()
    {
        //read spawnzone bounds
        var scaleX = _battleZoneTranform.transform.localScale.x;
        var scaleY = _battleZoneTranform.transform.localScale.y;

        //rng
        var x = Random.Range(-scaleX / 2, scaleX / 2);
        var y = Random.Range(1, 1 + (scaleY / 2));

        return _battleZoneTranform.position + new Vector3(x, y);
    }

    /// <summary>
    /// Spawns a player
    /// </summary>
    /// <param name="playerName">name to display on top of the player sprite</param>
    /// <param name="position">position to spawn player at</param>
    /// <returns>instance of the player, can only create 1 player per playerName</returns>
    public Player Spawn(string shipName, string playerName)
    {
        Debug.Log("2 Spawing " + playerName);

        //    //TODO: only allow 1 player instance?
        //    var existingPlayer = FindPlayerByName(playerName);
        //    if (existingPlayer != null) return null;

        //create instance of player prefab at position
        var instance = Instantiate(_playerPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        //set instance player name to display
        instance.InitialisePlayer(shipName, playerName);

        //register ondestroy hook that will stop tracking the player in the _players list
        instance.onDestroy += (self) => {
            _players.Remove(self);
        };

        //start tracking player instance
        _players.Add(instance);

        return instance;
    }

    [ContextMenu("Spawn player")]
    void SpawnDebug()
    {
        Spawn("HornCoom", "Player "+UnityEngine.Random.Range(0, float.MaxValue));
    }

    [ContextMenu("Spawn 10 players")]
    void Spawn10Debug()
    {
        for (int i = 0; i < 10; i++)
        {
            SpawnDebug();
        }
    }

    

    internal void BattleRoyaleStarting()
    {
        battleRoyaleState = BattleRoyaleState.SubmissionsClosed;

        foreach (Player p in _players) p.stateMachine.ChangeState(new PlayerSt_BattleRoyaleStarting());
    }



    internal int GetCrewCount() => int.Parse(_players.Sum(o => o.GetcrewCount()).ToString());
    internal int GetPlayerCount() => _players.Count();

    internal void BattleRoyaleAborted()
    {
        battleRoyaleState = BattleRoyaleState.NotTriggered;
    }

    internal void BattleRoyaleMustering()
    {
        battleRoyaleState = BattleRoyaleState.Mustering;
    }

    internal void BattleRoyaleStarted()
    {
        battleRoyaleState = BattleRoyaleState.InProgress;
        foreach (Player p in _players)
            p.stateMachine.ChangeState(new PlayerST_Fighting());
    }

    //TODO: maybe store players in dictionary for faster search access?
    //public Player FindPlayerByName(string name) =>
    //     _players.FirstOrDefault(x => !string.IsNullOrEmpty(x.GetCrewmate(name)));
}
//}
