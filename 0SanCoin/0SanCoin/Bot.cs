using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace _0SanCoin
{
    class Bot
    {
        DiscordSocketClient client; //봇 클라이언트
        CommandService commands;    //명령어 수신 클라이언트

        public async Task BotMain()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {    //디스코드 봇 초기화
                LogLevel = LogSeverity.Verbose                              //봇의 로그 레벨 설정 
            });
            commands = new CommandService(new CommandServiceConfig()        //명령어 수신 클라이언트 초기화
            {
                LogLevel = LogSeverity.Verbose                              //봇의 로그 레벨 설정
            });

            //로그 수신 시 로그 출력 함수에서 출력되도록 설정
            client.Log += OnClientLogReceived;
            commands.Log += OnClientLogReceived;

            await client.LoginAsync(TokenType.Bot, "TOKEN"); //봇의 토큰을 사용해 서버에 로그인
            await client.StartAsync();                         //봇이 이벤트를 수신하기 시작

            client.MessageReceived += OnClientMessage;         //봇이 메시지를 수신할 때 처리하도록 설정

            await Task.Delay(-1);   //봇이 종료되지 않도록 블로킹
        }

        private async Task OnClientMessage(SocketMessage arg)
        {
            //수신한 메시지가 사용자가 보낸 게 아닐 때 취소
            var message = arg as SocketUserMessage;
            if (message == null) return;

            int pos = 0;

            //메시지 앞에 !이 달려있지 않고, 자신이 호출된게 아니거나 다른 봇이 호출했다면 취소
            if (!(message.HasCharPrefix('!', ref pos) ||
             message.HasMentionPrefix(client.CurrentUser, ref pos)) ||
              message.Author.IsBot)
                return;

            var context = new SocketCommandContext(client, message);    //수신된 메시지에 대한 컨텍스트 생성   
            string[] search = message.Content.ToString().Split(' ');

            if (search[0] == "!회원가입")
            {
                if (getUserStalin(message.Author.Id.ToString()) == -1)
                {
                    register(message.Author.Id.ToString());
                    await context.Channel.SendMessageAsync($"성공적으로 회원가입이 되었습니다! \nid : {message.Author.Id.ToString()}");
                }
                else
                {
                    await context.Channel.SendMessageAsync("이미 회원가입을 하셨습니다");
                }
            }
            else if (search[0] == "!정보" || search[0] == "!돈")
            {
                int ret = getUserMoney(message.Author.Id.ToString());
                int ret2 = getUserStalin(message.Author.Id.ToString());
                if (ret == -1)
                {
                    await context.Channel.SendMessageAsync("먼저 회원가입을 해주세요");
                    await context.Channel.SendMessageAsync("``!회원가입`` 명령어로 회원가입을 할수있습니다");
                }
                else
                {
                    await context.Channel.SendMessageAsync($"> 이름 : {message.Author.Username} \n> 돈 :" +
                        $" " + ret.ToString() + "원\n"
                        + "> 공산코인 : " + ret2.ToString() + "개");
                }
            }
            else if(search[0] == "!설명")
            {
                await context.Channel.SendMessageAsync("``준비중인 명령어입니다``");
            }
            else if (search[0] == "!디버그")
            {
                await context.Channel.SendMessageAsync("F12");
            }
            else if (search[0] == "!공산")
            {
                await context.Channel.SendMessageAsync("> " + getCoinStalin().ToString() + "원");
            }
            else if (search[0] == "!공산구매")
            {
                int ret = getUserMoney(message.Author.Id.ToString());
                if (ret == -1)
                {
                    await context.Channel.SendMessageAsync("먼저 회원가입을 해주세요");
                    await context.Channel.SendMessageAsync("``!회원가입`` 명령어로 회원가입을 할수있습니다");
                }
                else
                {
                    if (search.Length == 1)
                    {
                        int money = getUserMoney(message.Author.Id.ToString());
                        int stalin = getUserStalin(message.Author.Id.ToString());
                        int coin = getCoinStalin();

                        setUserMoney(message.Author.Id.ToString(), money - ((money / coin) * coin));
                        //setUserMoney(message.Author.Id.ToString(), 0);
                        setUserStalin(message.Author.Id.ToString(), stalin + (money/coin));
                        await context.Channel.SendMessageAsync($"> 공산코인 {money/coin}개를 구입하였습니다\n> {(money / coin) * coin}원을 사용했습니다");

                        up0San(money / coin);
                    }
                    else if(search.Length == 2)
                    {
                        if(0 <= int.Parse(search[1]) && int.Parse(search[1]) <= 2147483647)
                        {
                            int money = getUserMoney(message.Author.Id.ToString());
                            int stalin = getUserStalin(message.Author.Id.ToString());
                            int coin = getCoinStalin();

                            if(money >= coin * int.Parse(search[1]))
                            {
                                setUserMoney(message.Author.Id.ToString(), money - (coin * int.Parse(search[1])));
                                setUserStalin(message.Author.Id.ToString(), stalin + int.Parse(search[1]));
                                await context.Channel.SendMessageAsync($"> 공산코인 {int.Parse(search[1])}개를 구입하였습니다\n> {coin * int.Parse(search[1])}원을 사용했습니다");

                                up0San(int.Parse(search[1]));
                            }
                            else
                            {

                                await context.Channel.SendMessageAsync($"> 돈이 부족합니다");
                            }
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync("거래량이 너무 크거나 작습니다 (0 ~ 2147483647)");
                        }
                    }
                }
            }
            else if (search[0] == "!공산판매")
            {
                int ret = getUserMoney(message.Author.Id.ToString());
                if (ret == -1)
                {
                    await context.Channel.SendMessageAsync("먼저 회원가입을 해주세요");
                    await context.Channel.SendMessageAsync("``!회원가입`` 명령어로 회원가입을 할수있습니다");
                }
                else
                {
                    if (search.Length == 1)
                    {
                        int money = getUserMoney(message.Author.Id.ToString());
                        int stalin = getUserStalin(message.Author.Id.ToString());
                        int coin = getCoinStalin();

                        setUserMoney(message.Author.Id.ToString(), money + (stalin * coin));
                        setUserStalin(message.Author.Id.ToString(), 0);
                        await context.Channel.SendMessageAsync($"> 공산코인 {stalin}개를 판매하였습니다\n> {stalin * coin}원을 얻었습니다");

                        down0San(stalin);
                    }
                    else if (search.Length == 2)
                    {
                        if (0 <= int.Parse(search[1]) && int.Parse(search[1]) <= 2147483647)
                        {
                            int money = getUserMoney(message.Author.Id.ToString());
                            int stalin = getUserStalin(message.Author.Id.ToString());
                            int coin = getCoinStalin();

                            if (stalin >= int.Parse(search[1]))
                            {
                                setUserMoney(message.Author.Id.ToString(), money + (coin * int.Parse(search[1])));
                                setUserStalin(message.Author.Id.ToString(), stalin - (int.Parse(search[1])));
                                await context.Channel.SendMessageAsync($"> 공산코인 {int.Parse(search[1])}개를 판매하였습니다\n> {coin * int.Parse(search[1])}원을 얻었습니다");
                                down0San(int.Parse(search[1]));
                            }
                            else
                            {

                                await context.Channel.SendMessageAsync($"> 보유한 코인개수가 부족합니다");
                            }
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync("거래량이 너무 크거나 작습니다 (0 ~ 2147483647)");
                        }
                    }
                }
            }
        }

        private Task OnClientLogReceived(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());  //로그 출력
            return Task.CompletedTask;
        }

        public void up0San(int cnt)
        {
            Random r = new Random();
            int per = r.Next(1, 11);
            int up = r.Next(1, 11);
            int down = r.Next(1, 21);

            int coin = getCoinStalin();

            if (per == 1)
            {
                if (coin - (up * cnt) >= 0)
                {
                    setCoinStalin(coin - down);
                }
                else
                {
                    setCoinStalin(1);
                }
            }
            else
            {
                if (coin + (up * cnt) <= 100)
                {
                    setCoinStalin(coin + up);
                }
                else
                {
                    setCoinStalin(100);
                }
            }
        }
        public void down0San(int cnt)
        {
            Random r = new Random();
            int per = r.Next(1, 11);
            int up = r.Next(1, 11);
            int down = r.Next(1, 21);

            int coin = getCoinStalin();

            if (per == 1)
            {
                if (coin + (up * cnt) <= 100)
                {
                    setCoinStalin(coin + up);
                }
                else
                {
                    setCoinStalin(100);
                }
            }
            else
            {
                if (coin - (up * cnt) >= 0)
                {
                    setCoinStalin(coin - up);
                }
                else
                {
                    setCoinStalin(1);
                }
            }
        }

        public void register(string name)
        {
            string strConn = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (SQLiteConnection conn = new SQLiteConnection(strConn))
            {
                conn.Open();
                string sql = $"insert into users values ('{name}', 500, 0)";
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                cmd.ExecuteNonQuery();

                //cmd.CommandText = "DELETE FROM member WHERE Id=1";
                //cmd.ExecuteNonQuery();
            }
        }

        public int getUserStalin(string id)
        {
            string connStr = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM users";

                //SQLiteDataReader를 이용하여 연결 모드로 데이타 읽기
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    //Console.WriteLine(rdr["name"]);
                    if (rdr["name"].ToString() == id)
                    {
                        string a = rdr["stalin"].ToString();
                        rdr.Close();
                        return int.Parse(a);
                    }
                }
                rdr.Close();
            }

            return -1;
        }

        public int getUserMoney(string id)
        {
            string connStr = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM users";

                //SQLiteDataReader를 이용하여 연결 모드로 데이타 읽기
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    //Console.WriteLine(rdr["name"]);
                    if (rdr["name"].ToString() == id)
                    {
                        string a = rdr["money"].ToString();
                        rdr.Close();
                        return int.Parse(a);
                    }
                }
                rdr.Close();
            }

            return -1;
        }

        public int setUserStalin(string id, int value)
        {
            string strConn = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (SQLiteConnection conn = new SQLiteConnection(strConn))
            {
                conn.Open();
                string sql = $"update users set stalin = {value} where name = {id}";
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                cmd.ExecuteNonQuery();

                //cmd.CommandText = "DELETE FROM member WHERE Id=1";
                //cmd.ExecuteNonQuery();
            }

            return 0;
        }

        public int setUserMoney(string id, int value)
        {
            string strConn = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (SQLiteConnection conn = new SQLiteConnection(strConn))
            {
                conn.Open();
                string sql = $"update users set money = {value} where name = {id}";
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                cmd.ExecuteNonQuery();

                //cmd.CommandText = "DELETE FROM member WHERE Id=1";
                //cmd.ExecuteNonQuery();
            }

            return 0;
        }

        public int getCoinStalin()
        {
            string co = "";
            string connStr = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM coin";

                //SQLiteDataReader를 이용하여 연결 모드로 데이타 읽기
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    co = rdr["stalin"].ToString();

                    rdr.Close();
                    return int.Parse(co);
                }
            }

            return int.Parse(co);
            //return int.Parse(co);
        }

        public int setCoinStalin(int value)
        {
            string strConn = @"Data Source=C:\Users\nfe81\Desktop\DataBase\data.db";

            using (SQLiteConnection conn = new SQLiteConnection(strConn))
            {
                conn.Open();
                string sql = $"update coin set stalin = {value}";
                SQLiteCommand cmd = new SQLiteCommand(sql, conn);
                cmd.ExecuteNonQuery();

                //cmd.CommandText = "DELETE FROM member WHERE Id=1";
                //cmd.ExecuteNonQuery();
            }

            return 0;
        }
    }
}
