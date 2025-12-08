using Allumeria.Rendering;

namespace Loader;

public static class LoadingAnimation
{
    private static Texture[] frames;
    private static float[] durations;
    private static int currentFrame = 0;
    private static float timeAccum = 0f;
    
    public static Texture CurrentFrame => frames[currentFrame];
    
    public static void Initialize()
    {
        frames = new Texture[12];
        for (int i = 0; i < 12; i++)
            frames[i] = new Texture(
                $"mods/res/loading/damn/DAMN_BIRD{i + 1}.png",
                flip: true,
                clamp: true,
                mipmaps: false);
        
        durations = new float[]
        {
            0.15f, 0.15f, 0.1f, 0.1f, 0.07f, 0.1f,
            0.1f, 0.1f, 0.07f, 0.1f, 0.1f, 0.1f
        };
    }
    
    public static void Update(float dt)
    {
        timeAccum += dt;

        if (timeAccum >= durations[currentFrame])
        {
            timeAccum -= durations[currentFrame];
            currentFrame++;

            if (currentFrame >= frames.Length)
                currentFrame = 0;
        }
    }
}