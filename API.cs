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

    public void InitMod(Mod mod)
    {
        ModEvents.GameUpdate.RegisterHandler(GameUpdate);
        // Maybe this happens automatically already?!
        // SingletonMonoBehaviour<SdtdConsole>.Instance.RegisterCommands();

    }

    private bool ShouldTimeProgress() {
        if (Overridden == SmartTimeOverride.FREEZE) {
            return false;
        } else if (Overridden == SmartTimeOverride.UNFREEZE) {
            return true;
        }

        World world = GameManager.Instance.World;

        return world.Players.list.Count >= minNumberOfPlayers;
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

    public void GameUpdate()
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

        ulong elapsed;
        if (UpdateWorldTime(out elapsed)) {
          UpdateDewCollectors(world, elapsed);
        }
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
