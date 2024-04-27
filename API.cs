using System.Collections.Generic;
using System.Reflection;

public class SmartTime : IModApi
{
    public enum SmartTimeOverride {
        DEFAULT,
        FREEZE,
        UNFREEZE
    }

    public static SmartTimeOverride Overridden = SmartTimeOverride.DEFAULT;
    int minNumberOfPlayers = 4;
    ulong lastWorldTime = 0;
    ulong accumulatedTimeUpdate = 0;
    const ulong FAKE_UPDATE_INTERVAL = 100;

    const int BLOOD_MOON_HOUR = 18;
    const int BLOOD_MOON_MINUTE = 0;

    const int DAY_END_HOUR = 23;
    const int DAY_END_MINUTE = 59;

    readonly ulong GRACE_TIME = GameUtils.TotalMinutesToWorldTime(5);

    public static SmartTime Instance { get; private set; } = null;

    public void InitMod(Mod mod)
    {
        ModEvents.GameUpdate.RegisterHandler(GameUpdate);
        ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
        Instance = this;
    }

    // GameUtils.WorldTimeToDays(ulong);
    // GameStats.GetInt(EnumGameStats.BloodMoonDay);

    private bool ShouldTimeProgress() {
        if (Overridden == SmartTimeOverride.FREEZE) {
            return false;
        } else if (Overridden == SmartTimeOverride.UNFREEZE) {
            return true;
        }

        World world = GameManager.Instance.World;

        return world.Players.list.Count == 0 || world.Players.list.Count >= minNumberOfPlayers;
    }

    private bool WasTimeProgressing = true;

    private void ChatSend(string message, List<int> recipients) {
        GameManager.Instance.ChatMessageServer(
                null, // Client info (null, since we're server)
                EChatType.Global,
                -1, // -1 since we're server
                message,
                "SmartTime", // Main Name
                false, // don't localize main name
                recipients);
    }

    private void ChatBroadcast(string message) {
        List<int> playerIds = new List<int>();
        foreach (var player in GameManager.Instance.World.Players.list) {
            if (player != null) {
                playerIds.Add(player.entityId);
            }
        }
        ChatSend(message, playerIds);
    }

    public void PlayerSpawnedInWorld(ClientInfo info, RespawnType respawnType, Vector3i pos) {
        if (respawnType != RespawnType.EnterMultiplayer && respawnType != RespawnType.JoinMultiplayer) {
            return;
        }

        InformPlayerOfStatus(info.entityId);
    }

    public void InformPlayerOfStatus(int entityId) {
        List<int> recipients = new List<int>();
        recipients.Add(entityId);

        ChatSend(ShouldTimeProgress() ? TimeUnfrozenMessage : TimeFrozenMessage, recipients);
    }

    private const string TimeUnfrozenMessage = "World time is [FF0000]UNFROZEN[-].";
    private void AnnounceTimeUnfrozen() {
        ChatBroadcast(TimeUnfrozenMessage);
    }

    private const string TimeFrozenMessage = "World time is [00FF00]FROZEN[-].";
    private void AnnounceTimeFrozen() {
        ChatBroadcast(TimeFrozenMessage);
    }

    private const string BloodMoonPlayersDisconnectMessage = "[FF0000] No longer enough players, but Blood Moon is in progress: Time will [b]not[/b] be frozen. Use stoptime to freeze.";
    private int lastPlayerCount = 0;
    public void GameUpdate() {
        World world = GameManager.Instance.World;
        var playerCount = world.Players.list.Count;

        // Reset override when all players are disconnected.
        if (playerCount == 0 && Overridden != SmartTimeOverride.DEFAULT) {
            Overridden = SmartTimeOverride.DEFAULT;
        }

        (int day, int hour, int minute) = GameUtils.WorldTimeToElements(world.worldTime);

        var bloodMoonDay = GameStats.GetInt(EnumGameStats.BloodMoonDay);

        if (playerCount != lastPlayerCount) {
            if (day == bloodMoonDay) {
                // It's blood moon day, don't reset.
                if (world.worldTime >= GameUtils.DayTimeToWorldTime(day, BLOOD_MOON_HOUR, BLOOD_MOON_MINUTE) - GRACE_TIME) {
                    if (playerCount >= minNumberOfPlayers && lastPlayerCount < minNumberOfPlayers) {
                        // No longer enough players.
                        // Do not automatically freeze!
                        if (Overridden == SmartTimeOverride.DEFAULT) {
                            Overridden = SmartTimeOverride.UNFREEZE;
                            ChatBroadcast(BloodMoonPlayersDisconnectMessage);
                        }
                    }
                }
            }
        }
        lastPlayerCount = playerCount;

        bool shouldProgress = ShouldTimeProgress();

        if (shouldProgress != WasTimeProgressing) {
            if (shouldProgress) {
                AnnounceTimeUnfrozen();
            } else {
                AnnounceTimeFrozen();
            }
        }
        WasTimeProgressing = shouldProgress;


        if (shouldProgress) {
            // Do nothing.
            return;
        }

        if (day == bloodMoonDay) {
            // Handle blood moon.
            if (world.worldTime >= GameUtils.DayTimeToWorldTime(day, BLOOD_MOON_HOUR, BLOOD_MOON_MINUTE) - GRACE_TIME) {
                // Back to 0:00.
                if (day == 0) {
                    world.SetTimeJump(GameUtils.DayTimeToWorldTime(day, 0, 0), false);
                } else {
                    world.SetTimeJump(GameUtils.DayTimeToWorldTime(day - 1, BLOOD_MOON_HOUR, BLOOD_MOON_MINUTE), false);
                }
            }
        } else {
            if (world.worldTime >= GameUtils.DayTimeToWorldTime(day, DAY_END_HOUR, DAY_END_MINUTE) - GRACE_TIME) {
                world.SetTimeJump(GameUtils.DayTimeToWorldTime(day, 0, 0), false);
            }
        }
    }

    /* Returns true iff time is frozen. */
    private bool UpdateWorldTime(out ulong elapsed) {
        World world = GameManager.Instance.World;
        if (lastWorldTime == 0) {
            lastWorldTime = world.worldTime;
        }

        elapsed = world.worldTime - lastWorldTime;
        if (!ShouldTimeProgress()) {
            world.SetTimeJump(lastWorldTime, false);
            return true;
        } else {
            lastWorldTime = world.worldTime;
            return false;
        }
    }

    public void GameUpdateOld()
    {
        World world = GameManager.Instance.World;
        if (world.Players.list.Count == 0) {
            if (Overridden != SmartTimeOverride.DEFAULT) {
                // Reset override as soon as all players are disconnected.
                Overridden = SmartTimeOverride.DEFAULT;
                lastWorldTime = 0;
            }

            return;
        }

        if (UpdateWorldTime(out var elapsed)) {
            FakeUpdate(world, elapsed);
        }
    }

    private void FakeUpdate(World world, ulong elapsed) {
        accumulatedTimeUpdate += elapsed;
        if (accumulatedTimeUpdate >= FAKE_UPDATE_INTERVAL) {
            PerformFakeUpdate(world, FAKE_UPDATE_INTERVAL);
            accumulatedTimeUpdate -= FAKE_UPDATE_INTERVAL;
        }
    }

    private void PerformFakeUpdate(World world, ulong totalElapsed) {
        UpdateDewCollectors(world, totalElapsed);
    }

    public static void ForceFreeze() {
        Overridden = SmartTimeOverride.FREEZE;
    }

    public static void ForceUnfreeze() {
        Overridden = SmartTimeOverride.UNFREEZE;
    }

    private void UpdateDewCollector(World world, TileEntityDewCollector dewCollector, ulong elapsed) {
        if (dewCollector != null) {
            System.Type dewType = dewCollector.GetType();
            FieldInfo dewTime = dewType.GetField("lastWorldTime",
                    BindingFlags.Instance |
                    BindingFlags.Public   |
                    BindingFlags.NonPublic);
            dewTime.SetValue(dewCollector, world.worldTime - elapsed);
            dewCollector.HandleUpdate(world);
        }
    }

    private void UpdateDewCollectors(World world, ulong elapsed) {
        if (elapsed == 0) {
          return;
        }

        List<Chunk> chunkArrayCopySync = world.ChunkCache.GetChunkArrayCopySync();
        for (int i = 0; i < chunkArrayCopySync.Count; i++) {
            var chunk = chunkArrayCopySync[i];
            var chunkWorldPos = chunk.GetWorldPos();
            chunk.LoopOverAllBlocks(delegate(int x, int y, int z, BlockValue blockValue) {
                var block = blockValue.Block;
                if (block is BlockDewCollector) {
                    var blockPos = new Vector3i(x, y, z);
                    TileEntity tileEntity = chunk.GetTileEntity(blockPos);
                    var dewCollector = tileEntity as TileEntityDewCollector;
                    UpdateDewCollector(world, dewCollector, elapsed);
                }
            });
        }
    }
}

public class ConsoleCmdStartTime : ConsoleCmdAbstract
{
    public override int DefaultPermissionLevel => 0;

    protected override string[] getCommands() {
        return new string[1]{ "starttime" };
    }

    protected override string getDescription() {
        return "Starts the time until all players are disconnected.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
        SmartTime.ForceUnfreeze();
    }
}

public class ConsoleCmdStopTime : ConsoleCmdAbstract
{
    public override int DefaultPermissionLevel => 0;

    protected override string[] getCommands() {
        return new string[1]{ "stoptime" };
    }

    protected override string getDescription() {
        return "Stops the time until all players are disconnected.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
        SmartTime.ForceFreeze();
    }
}

public class ConsoleCmdStatusTime : ConsoleCmdAbstract
{
    public override int DefaultPermissionLevel => 0;

    protected override string[] getCommands() {
        return new string[1]{ "statustime" };
    }

    protected override string getDescription() {
        return "Prints whether the smart time is frozen or unfrozen.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo) {
        int entityId = _senderInfo.RemoteClientInfo?.entityId ?? -1;
        if (entityId != -1) {
            SmartTime.Instance?.InformPlayerOfStatus(entityId);
        }
    }
}
