using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
class SubmarineWindow : GameWindow
{
    // משתנים גלובליים
    private bool isLightOn = true; // ברירת מחדל: הפנס דולק
    private int seabedTexture;
    private int textTexture;
    private int submarineTexture;
    private int fishTexture1, fishTexture2, fishTexture3;
    private Vector3 submarineDirection = new Vector3(0, 0, -1); // כיוון ברירת מחדל
    private Vector3 submarinePosition = new Vector3(0, -1, 0);  // מיקום התחלתי
    private float submarineScale = 0.5f; // קנה מידה של הצוללת
    private Vector3 cameraPosition = new Vector3(0, 10, 10); // מיקום המצלמה
    private Vector3 cameraDirection = new Vector3(0, -1, -1).Normalized(); // כיוון המצלמה
    private List<Vector3> bubbles = new List<Vector3>(); // רשימה שמחזיקה את כל הבועות
    private Random random = new Random(); // מחולל מספרים אקראיים בשביל בועות
    private List<Fish> fishList = new List<Fish>();
    private Random fishRandom = new Random();
    private float submarineRotation = 0.0f; // זווית הסיבוב של הצוללת
    private float moveSpeed = 0.1f; // מהירות התנועה
    private float rotationSpeed = 2.0f; // מהירות הסיבוב

    public SubmarineWindow() : base(800, 600, GraphicsMode.Default, "Submarine Mission") { }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        GL.ClearColor(0.0f, 0.0f, 0.3f, 1.0f); // רקע כחול כהה (מים)
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Lighting);
        GL.Enable(EnableCap.Light0);
        GL.Enable(EnableCap.Light1); // פנס הצוללת
        GL.Enable(EnableCap.ColorMaterial);
        GL.ShadeModel(ShadingModel.Smooth);
        GL.Enable(EnableCap.Texture2D); // הפעלת טקסטורות
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.AlphaTest);
        GL.AlphaFunc(AlphaFunction.Greater, 0.1f); // להעלים חלקים שקופים
        float[] fogColor = { 0.0f, 0.2f, 0.5f, 1.0f };
        GL.Fog(FogParameter.FogColor, fogColor);
        GL.Fog(FogParameter.FogMode, (int)FogMode.Exp2);
        GL.Fog(FogParameter.FogDensity, 0.03f); // חוזק הערפל
        GL.Enable(EnableCap.Fog);
        GL.Fog(FogParameter.FogMode, (int)FogMode.Exp2); // משתמש בפונקציה של ערפל מעריכי (Exp2)
        GL.Fog(FogParameter.FogDensity, 0.03f); // 📌 הצפיפות של הערפל – אפשר לשחק עם זה!
        GL.Fog(FogParameter.FogStart, 3.0f); // 📌 מאיפה מתחיל הערפל
        GL.Fog(FogParameter.FogEnd, 15.0f); // 📌 איפה הערפל הכי חזק
        GL.Fog(FogParameter.FogColor, new float[] { 0.0f, 0.2f, 0.4f, 1.0f }); // 📌 צבע ערפל (כחול-עמוק)
        GL.Enable(EnableCap.StencilTest); // ✅ מאפשר שימוש ב-Stencil Buffer
        GL.ClearStencil(0); // ✅ מאפס את ה-Stencil Buffer
        GL.StencilFunc(StencilFunction.Always, 1, 0xFF); // ✅ מאפשר כתיבה לכל פיקסל
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace); // ✅ קובע כיצד לערוך צללים


        GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f); // 📌 שינוי רקע שיתאים לערפל

        seabedTexture = LoadTexture(@"C:\Users\semer\source\repos\SunmarinMission\SunmarinMission\bin\debug\39.jpg"); ; // טעינת המרקם לקרקעית
        submarineTexture = LoadTexture(@"C:\Users\semer\source\repos\SunmarinMission\SunmarinMission\bin\debug\42.png"); // טוען את הטקסטורה של הצוללת
        fishTexture1 = LoadTexture(@"C:\Users\semer\source\repos\SunmarinMission\SunmarinMission\bin\debug\30fish.png");// וודא שהתמונה נמצאת בתיקייה הנכונה
        fishTexture2 = LoadTexture(@"C:\Users\semer\source\repos\SunmarinMission\SunmarinMission\bin\debug\31FISH.png");// וודא שהתמונה נמצאת בתיקייה הנכונה
        fishTexture3 = LoadTexture(@"C:\Users\semer\source\repos\SunmarinMission\SunmarinMission\bin\debug\32FISH.png");// וודא שהתמונה נמצאת בתיקייה הנכונה
        if (seabedTexture == -1 || submarineTexture == -1)
        {
            Console.WriteLine("❌ ERROR: One or more textures failed to load!");
        }
        else
        {
            Console.WriteLine($"✅ Textures loaded: Seabed={seabedTexture}, Submarine={submarineTexture}");
        }


        SetLighting();
    }


    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Width, Height);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60), Width / (float)Height, 0.1f, 500.0f);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref projection);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.Lighting);  
        GL.Enable(EnableCap.Light0);    
        GL.Enable(EnableCap.Light1);    
        GL.Enable(EnableCap.ColorMaterial); 


        UpdateCamera();
        if (isLightOn)
        {
           // DrawSubmarineShadow(); // ✅ מצייר את הצל של הצוללת
            DrawFishShadows(); // ✅ מצייר את הצל של הדגים
        }
        SetSubmarineLight();
        DrawSeabed();
        DrawSubmarine();
        DrawBubbles();
        DrawSeaCreatures();

        // בחירת צבע לפי מהירות
        System.Drawing.Color speedColor;
        if (moveSpeed <= 0.05f) speedColor = System.Drawing.Color.Blue;
        else if (moveSpeed > 0.05f && moveSpeed <= 0.1f) speedColor = System.Drawing.Color.White;
        else speedColor = System.Drawing.Color.Red;

        // יצירת טקסטורה לכל טקסט
        int depthTexture = GenerateTextTexture($"עומק: {Math.Round(submarinePosition.Y, 2)} מטרים", System.Drawing.Color.White);
        int speedTexture = GenerateTextTexture($"מהירות: {moveSpeed} מטר/שניה", speedColor);

        // ציור הטקסטים על המסך
        DrawTextTexture(depthTexture, 20, 550, 256, 64);
        DrawTextTexture(speedTexture, 20, 500, 256, 64);

        if (isLightOn) // ✅ צל מוצג רק אם הפנס דולק
        {
            DrawShadow(submarinePosition);
        }


        SwapBuffers();
    }



    private void DrawSubmarine()
    {
        GL.Enable(EnableCap.Lighting); // 📌 לוודא שהתאורה פועלת
        GL.Enable(EnableCap.Light0); // 📌 להפעיל את האור הכללי
        GL.Enable(EnableCap.Light1); // 📌 להפעיל את הפנס

        GL.PushMatrix();
        GL.Translate(submarinePosition);
        GL.Rotate(submarineRotation, 0, 1, 0);

        // הגדלת הצוללת פי 2 כדי שלא תיראה קטנה
        float scaleFactor = 2.0f;
        GL.Scale(submarineScale * scaleFactor, submarineScale * scaleFactor, submarineScale * scaleFactor);

        GL.Enable(EnableCap.Texture2D);
        GL.Enable(EnableCap.Blend);
        GL.BindTexture(TextureTarget.Texture2D, submarineTexture);

        // ציור הצוללת עם טקסטורה - רק צד אחד (כדי למנוע כפילות)
        GL.Begin(PrimitiveType.Quads);
        GL.Color3(1.0f, 1.0f, 1.0f);

        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-1.0f, -0.5f, 0.0f);
        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, -0.5f, 0.0f);
        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 0.5f, 0.0f);
        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-1.0f, 0.5f, 0.0f);

        GL.End();

        GL.Disable(EnableCap.Texture2D);
        GL.Disable(EnableCap.Blend);

        GL.PopMatrix();
    }


    private void DrawSubmarineDepth()
    {
        GL.Begin(PrimitiveType.Quads);

        // גב הצוללת
        GL.Vertex3(-0.8f, -0.4f, -0.1f);
        GL.Vertex3(0.8f, -0.4f, -0.1f);
        GL.Vertex3(0.8f, 0.4f, -0.1f);
        GL.Vertex3(-0.8f, 0.4f, -0.1f);

        GL.End();
    }


    private void DrawRoundedSubmarineEnds(float radius, float height, float zPos)
    {
        GL.Begin(PrimitiveType.TriangleFan);
        GL.Vertex3(0.0f, 0.0f, zPos); // מרכז הכיפה

        int segments = 20;
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * Math.PI / segments;
            float x = (float)(Math.Cos(angle) * radius);
            float y = (float)(Math.Sin(angle) * height);
            GL.Vertex3(x, y, zPos);
        }

        GL.End();
    }


    private void DrawCapsuleEnd(float radius, float height, float zPos)
    {
        GL.Begin(PrimitiveType.TriangleFan);
        GL.Vertex3(0.0f, 0.0f, zPos); // מרכז הכיפה

        int segments = 20;
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * Math.PI / segments;
            float x = (float)(Math.Cos(angle) * radius);
            float y = (float)(Math.Sin(angle) * height);
            GL.Vertex3(x, y, zPos);
        }

        GL.End();
    }



    private void DrawTexturedCylinder(float radius, float height, int segments)
    {
        GL.Begin(PrimitiveType.QuadStrip);
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2.0 * Math.PI / segments;
            float x = (float)(Math.Cos(angle) * radius);
            float z = (float)(Math.Sin(angle) * radius);
            float texX = (float)i / segments;

            GL.TexCoord2(texX, 0.0f); GL.Vertex3(x, -height / 2, z);
            GL.TexCoord2(texX, 1.0f); GL.Vertex3(x, height / 2, z);
        }
        GL.End();
    }


    private void DrawSeabed()
    {
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, seabedTexture);

        GL.Begin(PrimitiveType.Quads);
        GL.Color3(1.0f, 1.0f, 1.0f); // צבע לבן כדי למנוע עיוותים
        GL.Normal3(0.0f, 1.0f, 0.0f);

        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-10.0f, -2.0f, -10.0f);
        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(10.0f, -2.0f, -10.0f);
        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(10.0f, -2.0f, 10.0f);
        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-10.0f, -2.0f, 10.0f);

        GL.End();
        GL.Disable(EnableCap.Texture2D);
    }


    private void DrawCube(float size)
    {
        float half = size / 2;
        GL.Begin(PrimitiveType.Quads);

        GL.Vertex3(-half, -half, -half); GL.Vertex3(half, -half, -half); GL.Vertex3(half, half, -half); GL.Vertex3(-half, half, -half);
        GL.Vertex3(-half, -half, half); GL.Vertex3(half, -half, half); GL.Vertex3(half, half, half); GL.Vertex3(-half, half, half);

        GL.End();
    }

    private void DrawCylinder(float radius, float height, int segments)
    {
        GL.Begin(PrimitiveType.QuadStrip);
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2.0 * Math.PI / segments;
            double x = Math.Cos(angle) * radius;
            double z = Math.Sin(angle) * radius;

            GL.Vertex3(x, -height / 2, z);
            GL.Vertex3(x, height / 2, z);
        }
        GL.End();
    }
    private void DrawSeaCreatures()
    {
        GL.Enable(EnableCap.Texture2D);

        foreach (var fish in fishList)
        {
            GL.BindTexture(TextureTarget.Texture2D, fish.Texture);
            GL.PushMatrix();
            GL.Translate(fish.Position);

            // 📌 סיבוב הדג לכיוון התנועה
            float angle = (float)Math.Atan2(fish.Direction.X, fish.Direction.Z) * 180 / (float)Math.PI;
            GL.Rotate(angle, 0, 1, 0);

            DrawTexturedQuad(fish.Size, fish.Size * 0.5f);
            GL.PopMatrix();
        }

        if (isLightOn) // ✅ צל מוצג רק אם הפנס דולק
        {
            foreach (var fish in fishList)
            {
                DrawShadow(fish.Position);
            }
        }


        GL.Disable(EnableCap.Texture2D);
    }



    private void DrawTexturedQuad(float width, float height)
    {
        GL.Begin(PrimitiveType.Quads);

        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-width / 2, -height / 2, 0);
        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(width / 2, -height / 2, 0);
        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(width / 2, height / 2, 0);
        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-width / 2, height / 2, 0);

        GL.End();
    }

    private void DrawTail()
    {
        GL.Color3(1.0f, 0.5f, 0.2f); // צבע הכתום של הזנב
        GL.Begin(PrimitiveType.Triangles);

        GL.Vertex3(-0.1f, 0.05f, 0.0f);
        GL.Vertex3(-0.1f, -0.05f, 0.0f);
        GL.Vertex3(0.1f, 0.0f, 0.1f);

        GL.End();
    }



    private void UpdateCamera()
    {
        Matrix4 view = Matrix4.LookAt(
            submarinePosition + new Vector3(0, 2, 5),
            submarinePosition,
            Vector3.UnitY
        );
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadMatrix(ref view);
    }

    private void SetLighting()
    {
        GL.Enable(EnableCap.Lighting);
        GL.Enable(EnableCap.Light0);
        GL.Enable(EnableCap.Light1);

        float[] lightPosition = { 0.0f, 5.0f, 0.0f, 1.0f };
        float[] lightAmbient = { 0.05f, 0.05f, 0.1f, 1.0f };
        float[] lightDiffuse = { 0.3f, 0.3f, 0.6f, 1.0f };
        float[] lightSpecular = { 0.2f, 0.2f, 0.2f, 1.0f };

        GL.Light(LightName.Light0, LightParameter.Position, lightPosition);
        GL.Light(LightName.Light0, LightParameter.Ambient, lightAmbient);
        GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
        GL.Light(LightName.Light0, LightParameter.Specular, lightSpecular);

        // 📌 הפעלת מודל צללים רך
        GL.LightModel(LightModelParameter.LightModelAmbient, lightAmbient);
        GL.Enable(EnableCap.Normalize); // שומר על כיווני האור
    }



    private void SetSubmarineLight()
    {
        if (isLightOn)
        {
            GL.Enable(EnableCap.Light1);
            float[] spotlightPosition = { submarinePosition.X, submarinePosition.Y, submarinePosition.Z, 1.0f };
            float[] spotlightDirection = { submarineDirection.X, submarineDirection.Y, submarineDirection.Z };
            float[] spotlightDiffuse = { 2.0f, 2.0f, 2.0f, 1.0f };
            float[] spotlightSpecular = { 1.0f, 1.0f, 1.0f, 1.0f };
            float spotlightCutoff = 120.0f;
            float spotlightExponent = 0.5f;

            GL.Light(LightName.Light1, LightParameter.Position, spotlightPosition);
            GL.Light(LightName.Light1, LightParameter.SpotDirection, spotlightDirection);
            GL.Light(LightName.Light1, LightParameter.Diffuse, spotlightDiffuse);
            GL.Light(LightName.Light1, LightParameter.Specular, spotlightSpecular);
            GL.Light(LightName.Light1, LightParameter.SpotCutoff, spotlightCutoff);
            GL.Light(LightName.Light1, LightParameter.SpotExponent, spotlightExponent);

            Console.WriteLine("🔦 הפנס מופעל!");
        }
        else
        {
            GL.Disable(EnableCap.Light1); // כיבוי הפנס אם הוא כבוי
            Console.WriteLine("⚫ הפנס כבוי.");
        }
    }



    private int LoadTexture(string filePath)
    {
        try
        {
            Console.WriteLine($"🔍 Trying to load texture from: {filePath}");

            if (!File.Exists(filePath))
            {
                Console.WriteLine("❌ ERROR: Texture file not found!");
                return -1;
            }

            Bitmap bitmap = new Bitmap(filePath);
            Console.WriteLine($"✅ Image loaded successfully: {filePath}");

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY); // OpenGL קורא תמונות הפוך
            int texture;
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb); // לוודא טעינת שקיפות

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0,
                          OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);
            bitmap.Dispose();

            // מגדיר שקיפות
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

            Console.WriteLine($"✅ Texture loaded successfully. ID: {texture}");
            return texture;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR loading texture: {ex.Message}");
            return -1;
        }
    }

    private void DrawBubbles()
    {
        GL.Color3(0.8f, 0.8f, 1.0f); // צבע כחול בהיר לבועות

        foreach (var bubble in bubbles)
        {
            GL.PushMatrix();
            GL.Translate(bubble);
            DrawSphere(0.05f); // ציור בועה קטנה
            GL.PopMatrix();
        }
    }
    private void DrawBubble(float radius)
    {
        int segments = 12; // מוסיף יותר עיגוליות לבועה
        GL.Color4(0.6f, 0.8f, 1.0f, 0.5f); // צבע כחול בהיר עם שקיפות

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Begin(PrimitiveType.TriangleFan);
        GL.Vertex3(0.0f, 0.0f, 0.0f); // מרכז הבועה

        for (int i = 0; i <= segments; i++)
        {
            double angle = i * 2.0 * Math.PI / segments;
            float x = (float)(Math.Cos(angle) * radius);
            float y = (float)(Math.Sin(angle) * radius);
            GL.Vertex3(x, y, 0.0f);
        }

        GL.End();
        GL.Disable(EnableCap.Blend);
    }
    private void DrawShadow(Vector3 position)
    {
        if (!isLightOn) return; // ✅ הצל יופיע רק כשהפנס דולק

        GL.Disable(EnableCap.Lighting);  // ✅ מכבים תאורה כדי לצייר את הצל
        GL.Color4(0.0f, 0.0f, 0.0f, 0.6f); // ✅ צל שחור חצי שקוף (60% שקוף)

        GL.PushMatrix();
        GL.Translate(0.0f, -1.95f, 0.0f); // ✅ מקרינים את הצל על הקרקעית
        float[] lightPos = { submarinePosition.X, submarinePosition.Y + 5.0f, submarinePosition.Z, 1.0f };
        CreateShadowMatrix(lightPos); // ✅ שימוש במטריצת הקרנה משופרת

        DrawSubmarine(); // ✅ הקרנת צללים לפי הצוללת
        GL.PopMatrix();

        GL.Enable(EnableCap.Lighting);  // ✅ מחזירים תאורה
    }


    private void CreateShadowMatrix(float[] lightPos)
    {
        float groundPlaneY = -2.0f; // גובה הקרקעית

        // מטריצת הקרנה – מקרינה צל לפי מקור האור
        float[] shadowMatrix = new float[16];

        shadowMatrix[0] = lightPos[1] - groundPlaneY;
        shadowMatrix[4] = -lightPos[0];
        shadowMatrix[8] = -lightPos[2];
        shadowMatrix[12] = 0.0f;

        shadowMatrix[1] = 0.0f;
        shadowMatrix[5] = lightPos[1] - groundPlaneY;
        shadowMatrix[9] = 0.0f;
        shadowMatrix[13] = 0.0f;

        shadowMatrix[2] = 0.0f;
        shadowMatrix[6] = -lightPos[1];
        shadowMatrix[10] = lightPos[1] - groundPlaneY;
        shadowMatrix[14] = 0.0f;

        shadowMatrix[3] = 0.0f;
        shadowMatrix[7] = -1.0f;
        shadowMatrix[11] = 0.0f;
        shadowMatrix[15] = lightPos[1] - groundPlaneY;

        GL.MultMatrix(shadowMatrix);
    }
    private void DrawSubmarineShadow()
    {
        if (!isLightOn) return; // ✅ צללים מופיעים רק כשהפנס דולק

        GL.Disable(EnableCap.Lighting); // ❌ מכבים תאורה עבור הצל
        GL.Color4(0.0f, 0.0f, 0.0f, 0.5f); // ✅ צללים שקופים למחצה

        GL.Enable(EnableCap.StencilTest);
        GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

        GL.PushMatrix();
        GL.Translate(submarinePosition.X, -1.95f, submarinePosition.Z); // ✅ ממקמים את הצל קרוב לקרקעית
        GL.Scale(1.0f, 0.05f, 1.0f); // ✅ הצל שטוח על הקרקע
        DrawSubmarine(); // ✅ מציירים את הצוללת כצל
        GL.PopMatrix();

        GL.Disable(EnableCap.StencilTest);
        GL.Enable(EnableCap.Lighting); // ✅ מחזירים תאורה
    }
    private void DrawFishShadows()
    {
        if (!isLightOn) return; // ✅ צללים מופיעים רק אם הפנס דולק

        GL.Disable(EnableCap.Lighting);
        GL.Color4(0.0f, 0.0f, 0.0f, 0.4f); // ✅ צבע שחור שקוף למחצה

        foreach (var fish in fishList)
        {
            GL.PushMatrix();
            GL.Translate(fish.Position.X, -1.95f, fish.Position.Z); // ✅ מציבים את הצל בקרקעית
            GL.Scale(1.0f, 0.05f, 1.0f); // ✅ משטחים את הצל כמו בצל הצוללת
            DrawTexturedQuad(fish.Size, fish.Size * 0.5f); // ✅ משתמשים באותה פונקציה לציור הדג
            GL.PopMatrix();
        }

        GL.Enable(EnableCap.Lighting);
    }



    private void DrawSphere(float radius)
    {
        int segments = 10;
        GL.Begin(PrimitiveType.TriangleFan);
        GL.Vertex3(0.0f, 0.0f, 0.0f); // מרכז הספירה

        for (int i = 0; i <= segments; i++)
        {
            double angle = i * Math.PI / segments * Math.PI;
            float x = (float)(Math.Cos(angle) * radius);
            float y = (float)(Math.Sin(angle) * radius);
            GL.Vertex3(x, y, 0.0f);
        }

        GL.End();
    }

    private int GenerateTextTexture(string text, System.Drawing.Color color)
    {
        System.Drawing.Font font = new System.Drawing.Font("Arial", 20);
        System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(256, 64);
        System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bitmap);
        gfx.Clear(System.Drawing.Color.Transparent);
        gfx.DrawString(text, font, new System.Drawing.SolidBrush(color), 10, 10);

        // הפיכת התמונה למצב המתאים ל- OpenGL
        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

        System.Drawing.Imaging.BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb
        );

        int texture;
        GL.GenTextures(1, out texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0,
                      OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

        bitmap.UnlockBits(data);
        bitmap.Dispose();

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

        return texture;
    }
    private void DrawTextTexture(int texture, float x, float y, float width, float height)
    {
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.Begin(PrimitiveType.Quads);

        GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(x, y);
        GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(x + width, y);
        GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(x + width, y + height);
        GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(x, y + height);

        GL.End();
        GL.Disable(EnableCap.Texture2D);
    }





    private float waveOffset = 0.0f;
    private float fishSwimOffset = 0.0f;

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        var keyboard = Keyboard.GetState();

        // 📌 תנודה טבעית של הצוללת בגלים
        waveOffset += 0.05f;
        submarinePosition.Y += (float)Math.Sin(waveOffset) * 0.005f;

        // 📌 אתחול מהירות לפני חישובים
        moveSpeed = 0.0f; // ברירת מחדל: הצוללת לא נעה

        // 📌 סיבוב הצוללת (ימינה ושמאלה)
        if (keyboard.IsKeyDown(Key.A))
            submarineRotation += rotationSpeed;
        if (keyboard.IsKeyDown(Key.D))
            submarineRotation -= rotationSpeed;

        // 📌 עדכון כיוון הצוללת לפי הסיבוב
        float radians = MathHelper.DegreesToRadians(submarineRotation);
        submarineDirection = new Vector3((float)Math.Sin(radians), 0, (float)-Math.Cos(radians));

        // 📌 תנועה קדימה ואחורה - עדכון moveSpeed
        if (keyboard.IsKeyDown(Key.W))
        {
            submarinePosition += submarineDirection * 0.1f;
            moveSpeed = 0.1f; // הצוללת נעה קדימה
        }
        if (keyboard.IsKeyDown(Key.S))
        {
            submarinePosition -= submarineDirection * 0.1f;
            moveSpeed = 0.1f; // הצוללת נעה אחורה
        }

        // 📌 תנועה מעלה ומטה - עדכון moveSpeed
        if (keyboard.IsKeyDown(Key.Q))
        {
            submarinePosition.Y += 0.05f; // עלייה
            moveSpeed = 0.05f;
        }
        if (keyboard.IsKeyDown(Key.E))
        {
            submarinePosition.Y -= 0.05f; // ירידה
            moveSpeed = 0.05f;
        }

        // 📌 הדלקה וכיבוי של הפנס
        if (keyboard.IsKeyDown(Key.F))
        {
            isLightOn = !isLightOn;
            System.Threading.Thread.Sleep(200);
        }

        // 📌 יצירת בועות כשהצוללת נעה
        if (keyboard.IsKeyDown(Key.W) || keyboard.IsKeyDown(Key.S))
        {
            float bubbleOffsetX = (float)(random.NextDouble() * 0.2 - 0.1);
            float bubbleOffsetY = (float)(random.NextDouble() * 0.2 - 0.1);
            float bubbleZ = submarinePosition.Z - 0.5f;

            bubbles.Add(new Vector3(submarinePosition.X + bubbleOffsetX, submarinePosition.Y + bubbleOffsetY, bubbleZ));
        }

        // 📌 עדכון תנועת הבועות - הן עולות למעלה ונעלמות לאחר זמן מה
        for (int i = 0; i < bubbles.Count; i++)
        {
            bubbles[i] = new Vector3(bubbles[i].X, bubbles[i].Y + 0.02f, bubbles[i].Z);
            if (bubbles[i].Y > 2.0f)
            {
                bubbles.RemoveAt(i);
                i--;
            }
        }

        // 📌 יצירת דגים אם יש פחות מ-10
        if (fishList.Count < 10 && fishRandom.NextDouble() < 0.02)
        {
            float x = (float)(fishRandom.NextDouble() * 8 - 4);
            float y = (float)(fishRandom.NextDouble() * 3 - 1.5);
            float z = submarinePosition.Z - 5.0f;

            float speed = (float)(fishRandom.NextDouble() * 0.005f + 0.003f);
            float size = (float)(fishRandom.NextDouble() * 0.5f + 0.2f);

            // 📌 יצירת כיוון שחייה רנדומלי
            Vector3 direction = new Vector3(
                (float)(fishRandom.NextDouble() * 2 - 1),
                (float)(fishRandom.NextDouble() * 2 - 1),
                (float)(fishRandom.NextDouble() * -1)
            );

            int texture;
            int fishType = fishRandom.Next(3);
            if (fishType == 0)
                texture = fishTexture1;
            else if (fishType == 1)
                texture = fishTexture2;
            else
                texture = fishTexture3;


            fishList.Add(new Fish(new Vector3(x, y, z), direction, speed, size, texture));
        }

        // 📌 שיפור תנועת הדגים - שינוי כיוון מדי פעם
        fishSwimOffset += 0.1f;

        for (int i = 0; i < fishList.Count; i++)
        {
            Fish fish = fishList[i];

            // 📌 מדי פעם הדג ישנה כיוון קצת
            if (fishRandom.Next(100) < 3)
            {
                fish.Direction = new Vector3(
                    fish.Direction.X + (float)(fishRandom.NextDouble() * 0.1f - 0.05f),
                    fish.Direction.Y + (float)(fishRandom.NextDouble() * 0.05f - 0.025f),
                    fish.Direction.Z
                ).Normalized();
            }

            // 📌 תנועה טבעית - שחייה קדימה בכיוון מוגדר מראש
            Vector3 swimMovement = fish.Direction * fish.Speed;

            // 📌 תנודות קלות מצד לצד ולמעלה-למטה
            float swimMotionX = (float)Math.Sin(fishSwimOffset + i) * 0.02f;
            float swimMotionY = (float)Math.Sin(fishSwimOffset * 0.5f + i) * 0.015f;

            fish.Position += new Vector3(swimMovement.X + swimMotionX,
                                         swimMovement.Y + swimMotionY,
                                         swimMovement.Z);

            // 📌 אם הדג יוצא מהאזור הקרוב – מחזירים אותו לצד השני
            if (fish.Position.Z > submarinePosition.Z + 5.0f || fish.Position.Z < submarinePosition.Z - 15.0f)
            {
                fish.Position = new Vector3(
                    (float)(fishRandom.NextDouble() * 8 - 4),
                    (float)(fishRandom.NextDouble() * 3 - 1.5),
                    submarinePosition.Z - 10.0f
                );
            }

            fishList[i] = fish;
        }
    }



static void Main()
    {
        using (SubmarineWindow window = new SubmarineWindow())
        {
            window.Run(60.0);
        }
    }
}

class Fish
{
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; set; } // 📌 כיוון שחייה
    public float Speed { get; set; }
    public float Size { get; set; }
    public int Texture { get; set; }

    public Fish(Vector3 position, Vector3 direction, float speed, float size, int texture)
    {
        Position = position;
        Direction = direction.Normalized(); // 📌 נוודא שהכיוון הוא יחידה (אורך 1)
        Speed = speed;
        Size = size;
        Texture = texture;
    }
}



