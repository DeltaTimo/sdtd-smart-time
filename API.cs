using System.Collections.Generic;

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

    private bool UpdateWorldTime() {
        World world = GameManager.Instance.World;
        if (lastWorldTime == 0) {
            lastWorldTime = world.worldTime;
        }

        if (!ShouldTimeProgress()) {
            world.SetTimeJump(lastWorldTime, false);
            return false;
        } else {
            lastWorldTime = world.worldTime;
            return true;
        }
    }

    public void GameUpdate()
    {
        World world = GameManager.Instance.World;
        if (world.Players.list.Count == 0 && Overridden != SmartTimeOverride.DEFAULT) {
            // Reset override as soon as all players are disconnected.
            Overridden = SmartTimeOverride.DEFAULT;
        }

        UpdateWorldTime();
    }

    public static void ForceFreeze() {
        Overridden = SmartTimeOverride.FREEZE;
    }

    public static void ForceUnfreeze() {
        Overridden = SmartTimeOverride.UNFREEZE;
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
