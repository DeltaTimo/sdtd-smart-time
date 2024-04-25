public class API : IModApi
{
    int minNumberOfPlayers = 4;
    ulong lastWorldTime = 0;
    public void InitMod(Mod mod)
    {
       ModEvents.GameUpdate.RegisterHandler(GameUpdate);
    }

    public void GameUpdate()
    {
        World world = GameManager.Instance.World;
        
        if (lastWorldTime == 0) {
            lastWorldTime = world.worldTime;
            return;
        }

        int playerCount = world.Players.list.Count;

        if (playerCount < minNumberOfPlayers) {
            world.SetTimeJump(lastWorldTime, false);
        }

        lastWorldTime = world.worldTime;
    }
}
