using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LiteDB;

namespace InviteTracker.Commands;

public class SetLogChannel: Command
{
    public SetLogChannel(BotSettings settings): base(settings, PrepareCommand()) { }

    private static ApiCommand PrepareCommand()
    {
        return new ApiCommand()
        {
            name = "set_channel",
            description = "Set Channel where to post logs",
            type = CommandType.ChatInput,
            options = new List<ApiCommandOption>(new []
            {
                new ApiCommandOption()
                {
                    name = "channel",
                    description = "The channel",
                    required = true,
                    type = OptionType.Channel,
                    channelType = ChannelType.GuildText
                }
            })
        };
    }

    public override async Task Handle(DiscordClient sender, InteractionCreateEventArgs eventArgs)
    {
        if (eventArgs.Interaction.Data.Name == Name)
        {
            var member = await eventArgs.Interaction.Guild.GetMemberAsync(eventArgs.Interaction.User.Id);
            if (!member.Permissions.HasPermission(Permissions.BanMembers) && !member.Permissions.HasPermission(Permissions.Administrator))
            {
                var messageFail = new DiscordInteractionResponseBuilder().WithContent("You don't have enough permissions");
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, messageFail);
                return;
            }

            string? channelId = null;
                    
            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
                    
            foreach (var option in eventArgs.Interaction.Data.Options)
            {
                if (option.Name.Equals("channel"))
                {
                    channelId = option.Value.ToString();
                }
            }

            DiscordFollowupMessageBuilder reply;
            if (channelId is null)
            {
                reply = new DiscordFollowupMessageBuilder().WithContent("No Channel ID Found");
                reply.IsEphemeral = true;
                await eventArgs.Interaction.CreateFollowupMessageAsync(reply);
                return;
            }

            using (var db = new LiteDatabase(DbPath))
            {
                // Get or create Server "table"
                var collection = db.GetCollection<Server>("servers");

                var exists = collection.FindOne(x => x.ServerId == eventArgs.Interaction.Guild.Id.ToString());
                if (exists is null)
                {
                    // new one
                    var server = new Server()
                    {
                        ServerId = eventArgs.Interaction.Guild.Id.ToString(),
                        ChannelId = channelId
                    };
                    collection.Insert(server);
                    reply = new DiscordFollowupMessageBuilder().WithContent($"Channel <#{channelId}> set! :D");
                    reply.IsEphemeral = true;
                    await eventArgs.Interaction.CreateFollowupMessageAsync(reply);
                    return;
                }
                
                // update
                exists.ChannelId = channelId;
                collection.Update(exists);
            }

            reply = new DiscordFollowupMessageBuilder().WithContent($"Channel <#{channelId}> set! :D");
            reply.IsEphemeral = true;
            await eventArgs.Interaction.CreateFollowupMessageAsync(reply);
        }
    }
}