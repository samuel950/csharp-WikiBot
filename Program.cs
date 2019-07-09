using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;
using System.IO;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace WikiBot
{
    class Program
    {
        private DiscordSocketClient _client;

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
 
            _client = new DiscordSocketClient();
            _client.Log += Log;
            CommandHandler mch = new CommandHandler(_client, new CommandService());
            await mch.InstallCommandsAsync();
            //Bot token is stored as an environment variable for increased security.
            await _client.LoginAsync(TokenType.Bot,Environment.GetEnvironmentVariable("WikiBotToken",EnvironmentVariableTarget.Machine));
            await _client.StartAsync();
            
            await Task.Delay(-1);
            

        }
        //simple logger
        private Task Log(LogMessage msg)
        {

            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;

        }

    }

    class CommandHandler
    {

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {

            _commands = commands;
            _client = client;

        }

        //Loading commands...
        public async Task InstallCommandsAsync()
        {

            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),services: null);

        }

        private async Task HandleCommandAsync(SocketMessage messageParam){

            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            //'!' can be changed to any desired symbol/char.
            if(!(message.HasCharPrefix('!', ref argPos) ||
            message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
            message.Author.IsBot) return;

            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);

        }

    }

    public class WikiModule : ModuleBase<SocketCommandContext>
    {

        private readonly static string SEARCHURL ="https://en.wikipedia.org/wiki/Special:Search?search=";

        [Command("wiki")]
        [Summary("Searches Wikipedia for a term.")]
        public async Task WikiAsync([Remainder] [Summary("Term to search for.")] string term)
        {

            await WikiSearch(term);

        }

        /*
        The search works by attempting a search query on Wikipedia.
        If a search is "successful" it directly sends us to some sort of wiki page.
        If the search fails we are sent to a search results page.
        We can read the page source line by line to tell if the search was successful or if it failed.
         */
        private async Task WikiSearch(string search_term){

            string url = SEARCHURL + search_term.Replace(' ','+');
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
           
            if(response.StatusCode == HttpStatusCode.OK){
                //Console.WriteLine("Http Success!");
                TextReader reader = new StreamReader(response.GetResponseStream());
                string line = "";
                for(int i = 0; i < 5; i++){

                    line = reader.ReadLine();
                    
                }
                /*
                In the instance we land on a "Search results" 
                page it means that the search really wasn't successful.
                 */
                if(line.Contains("Search results")){
                    
                    /*
                    Building a nice embed to let the user know the search failed.
                     */
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Error!");
                    eb.WithDescription("Wiki page not found for ``" + search_term + "``!");
                    eb.WithColor(Color.Red);
                    await Context.Channel.SendMessageAsync(null,false,eb.Build());

                } else {

                    while((line = reader.ReadLine()) != null){
                        
                        //This line contains a direct link to the relevant Wikipedia page.
                        if(line.Contains("link rel=\"canonical\"")) 
                            break;

                    }
                    //Take the substring of the line that only contains the link.
                    string found = line.Substring(28,line.Length-31);
                    await Context.Channel.SendMessageAsync($"Found ``{search_term}``: {found}");

                }

                reader.Close();

            }

        }

    }

    public class UsageModule : ModuleBase<SocketCommandContext>
    {

        [Command("usage")]
        [Summary("Explains how to use WikiBot")]
        public async Task UsageAsync()
        {

            await Discord.UserExtensions.SendMessageAsync(Context.User
                ,"To use Wiki Bot type: ``!wiki -Search Term-`` (without the dashes)");
            
        }

    }

}
