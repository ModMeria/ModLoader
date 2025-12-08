using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Allumeria;
using Allumeria.Rendering;
using Allumeria.UI;
using Allumeria.UI.Text;
using HarmonyLib;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Loader;

public class LoadingScreenPatcher
{
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch("RenderLoadScreen")]
    public static class Game_RenderLoadScreen_Patch
    {
        [HarmonyPrefix]
        static bool Prefix()
        {
            return Drawing.uiTexture != null;
        }

        static void Postfix()
        {
            try
            {
                if (LoadingAnimation.CurrentFrame == null)
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                Logger.Warn("CurrentFrame still not loaded! Loading it.");
                LoadingAnimation.Initialize();
                return;
            }
            
            int size = 32;
            float padding = 10;

            float x = UIManager.scaledWidth  - size - padding;
            float y = UIManager.scaledHeight - size - padding;

            TextureBatcher.batcher.Start(LoadingAnimation.CurrentFrame);
            TextureBatcher.batcher.AddQuadScaled(
                200, 200, 48, 48, 0, 0, 48, 48, 2, TextureBatcher.colorWhite
                );
            TextureBatcher.batcher.Finalise();
            TextureBatcher.batcher.DrawBatch();
            
        }
    }
    
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch("OnLoad")]
    public static class Game_OnLoad_Patch
    {
        static Stopwatch timer = new Stopwatch();
        static Stopwatch animTimer = Stopwatch.StartNew();

        static void Prefix(Game __instance)
        {
            LoadingAnimation.Initialize();
            timer.Restart();
        }
        
        static void YieldFrame(Game game)
        {
            if (Game.threadedLoadDone) return;
            if (!IsUISafe()) return;
            Game.screenSize = game.ClientRectangle.Size;
            float dt = (float)animTimer.Elapsed.TotalSeconds;
            animTimer.Restart();
            LoadingAnimation.Update(dt);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            try
            {
                game.RenderLoadScreen();
                game.SwapBuffers();
                GLFW.PollEvents();
            }
            catch (Exception exception)
            {
                Logger.Warn("Not ready yet!");
                return;
            }
        }
        
        static bool IsUISafe()
        {
            if (Drawing.uiTexture == null)
                return false;
            
            if (TextureBatcher.batcher == null)
                return false;
            
            if (TextRenderer.currentFont == null)
                return false;
            
            if (GL.GetError() == OpenTK.Graphics.OpenGL.ErrorCode.InvalidOperation)
                return false;

            return true;
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int counter = 0;

            foreach (var code in instructions)
            {
                yield return code;
                counter++;

                if (counter >= 10)
                {
                    counter = 0;
                    Label skipLabel = il.DefineLabel();
                    
                    yield return new CodeInstruction(OpCodes.Ldsfld, 
                        AccessTools.Field(typeof(Game_OnLoad_Patch), nameof(timer)));
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(Stopwatch), nameof(Stopwatch.ElapsedMilliseconds)));
                    
                    yield return new CodeInstruction(OpCodes.Ldc_I4_S, 16);
                    yield return new CodeInstruction(OpCodes.Ble_S, skipLabel);
                    
                    
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Game_OnLoad_Patch), nameof(YieldFrame)));
                    
                    yield return new CodeInstruction(OpCodes.Ldsfld,
                        AccessTools.Field(typeof(Game_OnLoad_Patch), nameof(timer)));
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.Method(typeof(Stopwatch), nameof(Stopwatch.Restart)));
                    
                    CodeInstruction nop = new CodeInstruction(OpCodes.Nop);
                    nop.labels.Add(skipLabel);
                    yield return nop;
                }
            }
        }
    }
}