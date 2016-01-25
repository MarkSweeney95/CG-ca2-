using Microsoft.AspNet.SignalR.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using WebAPIAuthenticationClient;
using InputEngineNS;
using System.Threading.Tasks;
using GameClassLibrary;

namespace cg2015MonoGameClient
{
    
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D _txBackground;
        Texture2D _txCharacter;
        HubConnection connection;
        //Texture2D _txCollectable;
        Point _posCharacter = new Point(0, 0);
        IHubProxy proxy;
        SpriteFont font;
        double Exitcount = 10;
        bool GameOver = false;
        //List<string> TopScores = new List<string>();
        private bool _scoreboard;
        List<GameScoreObject> scores = new List<GameScoreObject>();
        List<string> chatMessages = new List<string>();

        // Set up a Viewport for the chat
        Viewport _chatvport;
        Viewport originalvport;
        IHubProxy chatproxy;
        bool chatMode = false;
        string line = string.Empty;

      

        static Random r = new Random();
        bool gameStarted = false;

        string clientID;
        Color playerColor = Color.White;
        Color enemyColor = Color.White;

        enum currentDisplay { Selection, Game, LeaderBoard, Score };
        currentDisplay currentState = currentDisplay.Selection;

        enum endGameStatuses { Win, Lose, Draw }
        endGameStatuses gameOutcome = endGameStatuses.Draw;

        Player player;
        Player Enemy;

        Menu menu;
        string[] menuOptions = new string[] { "Alan", "Thomas", "Mark", "Theo" };

        Vector2 startVector = new Vector2(50, 250);

        Bullet newBullet;

        int ammo = 1000;

        Texture2D backgroundTexture;
        Texture2D[] textures;
        Texture2D textureCollectable;
        Texture2D textureSuperCollectable;
        Texture2D[] textureBarrier;
        Texture2D texHealth;
        SpriteFont message;

        KeyboardState oldState, newState;

        public List<Bullet> Bullets = new List<Bullet>();
        List<Collectable> Collectables = new List<Collectable>();
        List<Barrier> Barriers = new List<Barrier>();
        List<Collectable> pickUp = new List<Collectable>();
        List<Barrier> destroyBarrier = new List<Barrier>();
        List<Bullet> destroyBullets = new List<Bullet>();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

       
        protected override void Initialize()
        {
            oldState = Keyboard.GetState();
            graphics.PreferredBackBufferWidth = 800; //set the size of the window
            graphics.PreferredBackBufferHeight = 600;
            graphics.ApplyChanges();


            proxy = connection.CreateHubProxy("GameHub");
            clientID = connection.ConnectionId;
            Action<string, string> RecivePlayer = recivePlayerMessage;
            Action<string, Vector2[]> ReciveBarriersPositions = reciveBarriers;
            Action<Vector2> ReciveNewPosition = reciveNewPlayerPosition;
            Action<string, Vector2, Vector2> ReciveNewBullet = reciveNewEnemyBullet;
            Action<Vector2> ReciveNewSuperCollectable = reciveSupercollectable;
            Action<List<Vector2>> ReciveCollectablePositions = reciveCollectablePositions;
            Action<Vector2> ReciveDiffrentStartposition = reciveDiffrentStartposition;

            proxy.On("sendPositionCollectables", ReciveCollectablePositions);
            proxy.On("sendBarriers", ReciveBarriersPositions);
            proxy.On("otherStartpoint", ReciveDiffrentStartposition);
            proxy.On("sendPlayer", RecivePlayer);
            proxy.On("updatePosition", ReciveNewPosition);
            proxy.On("newBullet", ReciveNewBullet);
            proxy.On("newSuperCollectable", ReciveNewSuperCollectable);

            new InputEngine(this);
            setupChatViewPort();
            // TODO: Add your initialization logic here
            //HubConnection connection = new HubConnection("http://cgmonogameserver2015.azurewebsites.net/");
            connection = new HubConnection("http://localhost:50574/");
            proxy = connection.CreateHubProxy("MoveCharacterHub");
            chatproxy = connection.CreateHubProxy("ChatHub");
            Action<Point> MoveRecieved = MovedRecievedMessage;
            proxy.On("setPosition", MoveRecieved);
            // Check Player Authentication constructor for endpoint setting
            // set to local host at the moment
            Task<bool> t = PlayerAuthentication.login("powell.paul@itsligo.ie", "itsPaul$1");
            t.Wait();
            Action<string, string> ChatRecieved = ChatRecievedMessage;
            chatproxy.On("heyThere", ChatRecieved);
            chatMessages.Add("Chat-->");
            connection.Start().Wait();
            base.Initialize();
        }

        private void setupChatViewPort()
        {
            originalvport = GraphicsDevice.Viewport;
            _chatvport = originalvport;
            _chatvport.Height = 200;
            _chatvport.X = 0;
            _chatvport.Y = originalvport.Height - _chatvport.Height;
        }

        private void ChatRecievedMessage(string from, string message)
        {

            chatMessages.Add(string.Concat(from, ":", message));
            chatMode = true;
        }

        private void MovedRecievedMessage(Point obj)
        {
            _posCharacter = obj;
        }


        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
       
            font = Content.Load<SpriteFont>("MessageFont");

            IsMouseVisible = true;
            spriteBatch = new SpriteBatch(GraphicsDevice);

            message = Content.Load<SpriteFont>("SpriteFont\\message"); //load spriteFont

     

            backgroundTexture = Content.Load<Texture2D>("Background\\Bck_stars");
            textures = new Texture2D[] { Content.Load<Texture2D>("Sprites\\AH_ship"), Content.Load<Texture2D>("Sprites\\tcShip"), Content.Load<Texture2D>("Sprites\\msShip"), Content.Load<Texture2D>("Sprites\\Th_Ship") };
            textureBarrier = new Texture2D[] { Content.Load<Texture2D>("Background\\Bck_Barrier"), Content.Load<Texture2D>("Background\\Bck_Barrier") };
            textureCollectable = Content.Load<Texture2D>("Sprites\\ammo");
            textureSuperCollectable = Content.Load<Texture2D>("Sprites\\ammo");
            texHealth = Content.Load<Texture2D>("Sprites\\healthBar");

          

            Console.WriteLine("////////////////Connecting///////////////////");
            connection.Start().Wait();
            Console.WriteLine("////////////////Connected///////////////////");



            for (int i = 0; i < 3; i++) //create barriers 
            {
                Barriers.Add(new Barrier(clientID, textureBarrier, new Vector2(r.Next(50, graphics.GraphicsDevice.Viewport.Width - 50), r.Next(50, graphics.GraphicsDevice.Viewport.Height - 50)), playerColor));
            }

            menu = new Menu(new Vector2(300, 250), menuOptions, message, textures); //create the menu

            menu.Active = true; //set menu active


            // TODO: use this.Content to load your game content here
        }

       
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            // Turn on chat mode
            if (InputEngine.IsKeyPressed(Keys.F1))
            {
                // line represent the current line to be captured
                line = string.Empty;
                chatMode = !chatMode;
            }
            // if chatting then do not update game window
            if (chatMode)
            {
                if (InputEngine.IsKeyPressed(Keys.Enter))
                {
                    // replace connection id with name of logged in player
                    chatproxy.Invoke("SendMess", new object[] { connection.ConnectionId, line });
                    line = string.Empty;
                    //chatMessages.Add(line);
                }
                else
                {
                    //if (InputEngine.PressedKeys.Length > 0)

                    if (InputEngine.currentKey != Keys.None)
                        line += InputEngine.lookupKeys[InputEngine.currentKey];
                }
            }
            // update game window
            else
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                    proxy.Invoke("GetPoint");
                if (!(PlayerAuthentication.PlayerStatus == AUTHSTATUS.OK))
                {
                    Exitcount -= gameTime.ElapsedGameTime.TotalSeconds;
                    if (Exitcount < 1)
                        Exit();
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    scores = PlayerAuthentication.getScores(5, "Battle Call");
                    if (scores != null)
                    {
                        _scoreboard = true;

                    }
                }
            }

            newState = Keyboard.GetState(); //set the current keyboardState

           

            if (currentState == currentDisplay.Selection)
            {
                menu.CheckMouse();

                player = createPlayer(clientID, menu.MenuAction, playerColor);

                if (player != null)
                {
                    proxy.Invoke("SendPlayer", menu.MenuAction);

                    sendBarriers(Barriers);
                }

                menu.MenuAction = null; //reset the selection
            }

         




            if (currentState == currentDisplay.Game) //if the game is running
            {
                if (gameStarted)
                {
                    if (newState.IsKeyDown(Keys.Escape) && oldState != newState && gameStarted)
                    {

                        currentState = currentDisplay.LeaderBoard;

                    }


                    player.Move(newState); //check for the player movement
                    proxy.Invoke("UpdatePosition", player._position);

                   
                    foreach (var item in Bullets) //check if bullet hit a barrier and destroy it
                    {
                        foreach (var bar in Barriers)
                        {
                            if (item.CollisiionDetection(bar.Rectangle))
                            {
                                if (item.createdPlayerID != bar.createdClientID)
                                {
                                    bar.GotHit(item);
                                    item.IsVisible = false;
                                    destroyBullets.Add(item);
                                    if (!bar.IsVisible)
                                        destroyBarrier.Add(bar);

                                }
                            }
                        }
                        if (item.CollisiionDetection(Enemy.Rectangle))
                            Enemy.PlayerChar.GotShoot(item);

                        if (item.CollisiionDetection(player.Rectangle))
                            player.PlayerChar.GotShoot(item);
                    }

                    foreach (var item in Collectables)
                    {
                        if (player.CollisiionDetection(item.Rectangle))
                        {
                            pickUp.Add(item);
                            ammo += 100;
                            item.IsVisible = false;
                            player.Collect(item);
                        }

                        if (Enemy.CollisiionDetection(item.Rectangle))
                        {
                            pickUp.Add(item);
                            item.IsVisible = false;
                            Enemy.Collect(item);
                        }
                    }



                    if (newState.IsKeyDown(Keys.Space) && oldState != newState && gameStarted)
                    {
                        if (ammo > 0)
                        {

                            newBullet = player.PlayerChar.Shoot(player._position, player.FireDirection, playerColor); //create a bullet
                            if (newBullet != null)
                            {
                                Bullets.Add(newBullet); //add the new bullet to the list
                                proxy.Invoke("NewBullet", newBullet._position, newBullet.flyDirection);
                                ammo -= 100;
                            }

                        }

                    }
                    //Bullets.Add(new Bullet(player.PlayerChar._texture, player.PlayerChar.strength, player.Position, player.FireDirection));

                    foreach (var item in Bullets)
                    {
                        item.Update(); //update the Bullets
                        if (OutsideScreen(item))
                        {
                            destroyBullets.Add(item);
                        }
                    }

                    foreach (var item in destroyBarrier)
                    {
                        Barriers.Remove(item);
                    }
                    foreach (var item in pickUp)
                    {
                        Collectables.Remove(item);
                    }
                    foreach (var item in destroyBullets)
                    {
                        Bullets.Remove(item);
                    }

                    destroyBarrier.Clear();
                    pickUp.Clear();
                    destroyBullets.Clear();

                    if (Collectables.Count == 0)
                        currentState = currentDisplay.Score;
                    if (Enemy.PlayerChar.Health <= 0)
                        currentState = currentDisplay.Score;
                    if (player.PlayerChar.Health <= 0)
                        currentState = currentDisplay.Score;

                    if (currentState == currentDisplay.Score)
                    {
                        gameStarted = false;
                        proxy.Invoke("StartGame", gameStarted);
                        if (player.score > Enemy.score)
                            gameOutcome = endGameStatuses.Win;
                        if (player.score < Enemy.score)
                            gameOutcome = endGameStatuses.Lose;
                        if (player.score == Enemy.score)
                            gameOutcome = endGameStatuses.Draw;
                    }

                }
            }



            if (newState.IsKeyDown(Keys.Escape) && oldState != newState) // go back to the character selection
                Exit();

            // TODO: Add your update logic here
            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            // Set the Viewport to the original
            GraphicsDevice.Viewport = originalvport;
            GraphicsDevice.Clear(Color.CornflowerBlue);
            if (chatMode)
            {
                // Set the Viewport to the chat port, show any lines of text recorded
                GraphicsDevice.Viewport = _chatvport;
                spriteBatch.Begin();
                Vector2 pos = Vector2.Zero;
                foreach (string chatline in chatMessages)
                {
                    spriteBatch.DrawString(font, chatline, pos, Color.White);
                    pos += new Vector2(0, font.MeasureString(chatline).Y);

                }
                // write current line
                if (line.Length > 0)
                    spriteBatch.DrawString(font, line, pos, Color.White);
                spriteBatch.End();
            }

            if (currentState == currentDisplay.Selection)
                menu.Draw(spriteBatch); //draw the menu



            // Set the Viewport to the original and show the game play
            GraphicsDevice.Viewport = originalvport;
            spriteBatch.Begin();
            if ((PlayerAuthentication.PlayerStatus == AUTHSTATUS.OK))
            {
                if (currentState == currentDisplay.Game) //if game is started
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, 800, 600), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f); //draw the background
                    if (Enemy != null)
                        spriteBatch.DrawString(message, "Score: " + Enemy.score.ToString(), new Vector2(700, 0), enemyColor);
                    spriteBatch.DrawString(message, "Score: " + ammo, new Vector2(0, 0), playerColor);
                    spriteBatch.End();

                    if (Enemy != null)
                        Enemy.Draw(spriteBatch);

                    player.Draw(spriteBatch, message); //draw the player

                    foreach (var item in Collectables)
                    {
                        item.Draw(spriteBatch); // draw the Collectabels at layer 0
                    }

                    foreach (var item in Bullets)
                    {
                        item.Draw(spriteBatch); // draw the Bullets
                    }

                    foreach (var item in Barriers)
                    {
                        item.Draw(spriteBatch); //draw the Barriers at layer 1
                    }
                }

                if (_scoreboard)
                {
                    Vector2 position = new Vector2(400, 200);
                    foreach (var item in scores)
                    {
                        spriteBatch.DrawString(font, item.GamerTag + ":" + item.score, position, Color.White);
                        position += new Vector2(0, 40);
                    }
                }

            }
            else
                spriteBatch.DrawString(font, "Exiting in " + ((int)Exitcount).ToString() + " owing to " + PlayerAuthentication.PlayerStatus.ToString(), new Vector2(10, 10), Color.White);
            spriteBatch.End();

            if (currentState == currentDisplay.Score)
            {
                player._position = new Vector2(300, 400);
                Enemy._position = new Vector2(450, 400);

                player.Draw(spriteBatch);
                Enemy.Draw(spriteBatch);
                Vector2 fontPos = new Vector2(player._texture.Width / 2, -10);
                Vector2 namePos = new Vector2(player._texture.Width / 2, player._texture.Height + 10);

                spriteBatch.Begin();
                spriteBatch.DrawString(message, gameOutcome.ToString(), new Vector2(350, 100), Color.BlueViolet, 0, Vector2.Zero, 3f, SpriteEffects.None, 0);
                spriteBatch.DrawString(message, player.score.ToString(), fontPos + player._position, playerColor, 0, message.MeasureString(player.score.ToString()) / 2, 1, SpriteEffects.None, 0);
                spriteBatch.DrawString(message, Enemy.score.ToString(), fontPos + Enemy._position, enemyColor, 0, message.MeasureString(Enemy.score.ToString()) / 2, 1, SpriteEffects.None, 0);

                spriteBatch.DrawString(message, "You", namePos + player._position, playerColor, 0, message.MeasureString("You") / 2, 1, SpriteEffects.None, 0);
                spriteBatch.DrawString(message, "Enemy", namePos + Enemy._position, enemyColor, 0, message.MeasureString("Enemy") / 2, 1, SpriteEffects.None, 0);
                spriteBatch.End();
            


                // TODO: Add your drawing code here

                base.Draw(gameTime);
            }

        }



        private void reciveSupercollectable(Vector2 obj)
        {
            Collectables.Add(new SuperCollectable(textureSuperCollectable, obj)); //create Testing SuperCollectable
        }

        private void reciveNewEnemyBullet(string arg1, Vector2 arg2, Vector2 arg3)
        {
            Bullets.Add(new Bullet(arg1, Enemy.PlayerChar._texture, Enemy.PlayerChar.strength, arg2, arg3, enemyColor));
        }

        private void reciveNewPlayerPosition(Vector2 obj)
        {
            Enemy._position = obj;
        }

        private void reciveBarriers(string arg1, Vector2[] arg2)
        {
            foreach (var item in arg2)
            {
                Barriers.Add(new Barrier(arg1, textureBarrier, item, enemyColor));
            }
        }

        private void recivePlayerMessage(string arg1, string arg2)
        {
            Enemy = createPlayer(arg1, arg2, enemyColor);
            gameStarted = true;
            //proxy.Invoke("GameStart", gameStarted);
        }

        private Player createPlayer(string id, string type, Color c)
        {
            Player temp = null;
            if (type != null)
            {

                switch (type.ToUpper()) //check for type and create the character
                {
                    case "ALAN":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Character(id, textures[0], 7, 3), texHealth, startVector, c, this);
                        break;
                    case "THOMAS":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Character(id, textures[1], 5, 4), texHealth, startVector, c, this);
                        break;
                    case "MARK":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Character(id, textures[2], 3, 5), texHealth, startVector, c, this);
                        break;
                    case "THEO":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Character(id, textures[3], 3, 5), texHealth, startVector, c, this);
                        break;
                    default:
                        break;
                }
            }


            return temp;
        }

        private void reciveCollectablePositions(List<Vector2> obj)
        {
            foreach (var item in obj)
            {
                Collectables.Add(new Collectable(textureCollectable, item));
            }
        }

        private void reciveDiffrentStartposition(Vector2 obj)
        {
            player._position = obj;
        }

        public bool OutsideScreen(Sprite obj)
        {
            if (!obj.Rectangle.Intersects(Window.ClientBounds))
            {
                return true;
            }
            else
                return false;
        }

        private void sendBarriers(List<Barrier> barriers)
        {
            List<Vector2> temp = new List<Vector2>();
            foreach (var item in barriers)
            {
                temp.Add(item._position);
            }

            proxy.Invoke("SendBarriers", temp);
        }

    }

}

