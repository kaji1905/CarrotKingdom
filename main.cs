using System;
using SDL2;

struct Screen
{
    public const int w = 1000;
    public const int h = 600;
    public const int frame_w = 20;
    public const int frame_h = 32;
}

namespace Ep4
{
    class Program
    {
        enum KeyPressSurfaces
        {
            KPS_Up = 0,
            KPS_Down = 1,
            KPS_Left = 2,
            KPS_Right = 3,
            KPS_Total = 4
        }

        static IntPtr gWindow = IntPtr.Zero;
        static IntPtr gScreenSurface = IntPtr.Zero;
        static IntPtr gSpriteSheet = IntPtr.Zero;

        static KeyPressSurfaces gCurrentDirection = KeyPressSurfaces.KPS_Down;
        static int currentFrame = 0; // Frame hiện tại (0, 1, 2, 3)

        static bool Init()
        {
            bool success = true;

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine("SDL could not initialize! SDL_Error: {0}", SDL.SDL_GetError());
                success = false;
            }
            else
            {
                gWindow = SDL.SDL_CreateWindow("Frisk 4x4 Grid Test", 
                    SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 
                    Screen.w, Screen.h, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

                if (gWindow == IntPtr.Zero)
                {
                    Console.WriteLine("Window could not be created! SDL_Error: {0}", SDL.SDL_GetError());
                    success = false;
                }
                else
                {
                    gScreenSurface = SDL.SDL_GetWindowSurface(gWindow);
                }
            }
            return success;
        }

        static IntPtr LoadSurface(string path)
        {
            IntPtr loadedSurface = SDL_image.IMG_Load(path);
            if (loadedSurface == IntPtr.Zero)
            {
                Console.WriteLine("Unable to load img {0}! SDL_image Error: {1}", path, SDL.SDL_GetError());
            }
            return loadedSurface;
        }

        static bool LoadMedia()
        {
            bool success = true;
            string spritePath = "assets/test_sprite.png";
            gSpriteSheet = LoadSurface(spritePath);

            if (gSpriteSheet == IntPtr.Zero)
            {
                Console.WriteLine("Failed to load sprite sheet {0}!", spritePath);
                success = false;
            }

            return success;
        }

        static void Close()
        {
            if (gSpriteSheet != IntPtr.Zero)
            {
                SDL.SDL_FreeSurface(gSpriteSheet);
                gSpriteSheet = IntPtr.Zero;
            }

            if (gWindow != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(gWindow);
                gWindow = IntPtr.Zero;
            }

            SDL_image.IMG_Quit();
            SDL.SDL_Quit();
        }

        static void Main(string[] args)
        {
            if (!Init())
            {
                Console.WriteLine("Failed to initialize!");
            }
            else
            {
                if (!LoadMedia())
                {
                    Console.WriteLine("Failed to load media!");
                }
                else
                {
                    bool quit = false;
                    uint lastTime = SDL.SDL_GetTicks();
                    float frameTimer = 0f;
                    float timePerFrame = 0.15f; // Chuyển frame sau mỗi 0.15s
                    bool isMoving = false;

                    while (!quit)
                    {
                        uint currentTime = SDL.SDL_GetTicks();
                        float deltaTime = (currentTime - lastTime) / 1000.0f;
                        lastTime = currentTime;

                        isMoving = false;

                        while (SDL.SDL_PollEvent(out var e) != 0)
                        {
                            if (e.type == SDL.SDL_EventType.SDL_QUIT)
                            {
                                quit = true;
                            }
                        }

                        // Lấy trạng thái phím liên tục
                        IntPtr keyState = SDL.SDL_GetKeyboardState(out _);
                        byte[] keys = new byte[256];
                        System.Runtime.InteropServices.Marshal.Copy(keyState, keys, 0, 256);

                        if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_UP] != 0 || keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_W] != 0)
                        {
                            gCurrentDirection = KeyPressSurfaces.KPS_Up;
                            isMoving = true;
                        }
                        else if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_DOWN] != 0 || keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_S] != 0)
                        {
                            gCurrentDirection = KeyPressSurfaces.KPS_Down;
                            isMoving = true;
                        }
                        else if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_LEFT] != 0 || keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_A] != 0)
                        {
                            gCurrentDirection = KeyPressSurfaces.KPS_Left;
                            isMoving = true;
                        }
                        else if (keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_RIGHT] != 0 || keys[(int)SDL.SDL_Scancode.SDL_SCANCODE_D] != 0)
                        {
                            gCurrentDirection = KeyPressSurfaces.KPS_Right;
                            isMoving = true;
                        }

                        // Xử lý chuyển frame đi bộ (Lặp từ frame 0 đến 3)
                        if (isMoving)
                        {
                            frameTimer += deltaTime;
                            if (frameTimer >= timePerFrame)
                            {
                                frameTimer -= timePerFrame;
                                currentFrame = (currentFrame + 1) % 4; // Vòng lặp 4 frame
                            }
                        }
                        else
                        {
                            currentFrame = 0; // Trở về frame đứng yên khi không bấm phím
                            frameTimer = 0f;
                        }

                        // Tô màu nền
                        SDL.SDL_Surface surface = (SDL.SDL_Surface)System.Runtime.InteropServices.Marshal.PtrToStructure(gScreenSurface, typeof(SDL.SDL_Surface));
                        uint backColor = SDL.SDL_MapRGB(surface.format, 214, 255, 163);
                        SDL.SDL_FillRect(gScreenSurface, IntPtr.Zero, backColor);

                        // Tính tọa độ cắt cho Grid 4x4 chuẩn:
                        // X = frame * 20
                        // Y = hàng_hướng * 32
                        SDL.SDL_Rect srcRect;
                        srcRect.x = currentFrame * Screen.frame_w;
                        srcRect.y = (int)gCurrentDirection * Screen.frame_h;
                        srcRect.w = Screen.frame_w;
                        srcRect.h = Screen.frame_h;

                        // Vẽ lên giữa màn hình với scale x2
                        int scale = 2;
                        SDL.SDL_Rect dstRect;
                        dstRect.w = Screen.frame_w * scale;
                        dstRect.h = Screen.frame_h * scale;
                        dstRect.x = (Screen.w - dstRect.w) / 2;
                        dstRect.y = (Screen.h - dstRect.h) / 2;

                        SDL.SDL_BlitScaled(gSpriteSheet, ref srcRect, gScreenSurface, ref dstRect);
                        SDL.SDL_UpdateWindowSurface(gWindow);
                    }
                }
            }

            Close();
        }
    }
}