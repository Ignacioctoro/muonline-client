using Client.Main.Core.Client;
using MUnique.OpenMU.Network.Packets.ServerToClient;

static void Check(bool valid, string message)
{
    if (!valid) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
var journal = new Client.Main.Core.Client.QuestJournal();
var availableBytes = new byte[AvailableQuests.GetRequiredSize(1)];
var available = new AvailableQuests(availableBytes) { QuestNpcNumber = 257, QuestCount = 1 };
var offer = available[0]; offer.Group = 18; offer.Number = 0;
journal.Apply(availableBytes);
Check(journal.Available.Single().Number == 0 && journal.Active.Count == 0, "NPC offers do not accept quests");
var stepBytes = new byte[QuestStepInfo.Length];
_ = new QuestStepInfo(stepBytes) { QuestGroup = 18, QuestStepNumber = 0 };
journal.Apply(stepBytes);
Check(journal.OfferedStep == (18, 0), "NPC starting step authorizes Accept button");
var progressBytes = new byte[QuestProgress.Length];
var progress = new QuestProgress(progressBytes) { QuestGroup = 18, QuestNumber = 1, ConditionCount = 1, RewardCount = 1 };
var condition = progress.GetQuestCondition(0);
condition.Type = ConditionType.MonsterKills; condition.RequirementId = 3;
condition.RequiredCount = 10; condition.CurrentCount = 4;
var reward = progress.GetQuestReward(0);
reward.Type = RewardType.Experience; reward.RewardCount = 2000;
progressBytes[12] = 0xFF; // Unspecified padding must never become part of the enum.
journal.Apply(progressBytes);
Check(journal.Active.Single().Number == 1 && journal.OfferedStep == null, "Server progress confirms quest acceptance");
Check(journal.Details[18].Conditions[0].Type == 1 && journal.Details[18].Conditions[0].Current == 4, "S6 condition type ignores padding and reads progress");
Check(journal.Details[18].Rewards[0].Count == 2000, "Server reward amount preserved");
var completionBytes = new byte[QuestCompletionResponse.Length];
var completion = new QuestCompletionResponse(completionBytes) { QuestGroup = 18, QuestNumber = 1, IsQuestCompleted = false };
journal.Apply(completionBytes);
Check(journal.Active.Count == 1, "Failed completion preserves quest");
completion.IsQuestCompleted = true; journal.Apply(completionBytes);
Check(journal.Active.Count == 0 && journal.Details.Count == 0, "Confirmed completion removes quest and details");
progressBytes[3] = 0x1B; journal.Apply(progressBytes);
Check(journal.Active.Count == 0, "Late detail response does not resurrect completed quest");
progressBytes[3] = 0x0C; journal.Apply(progressBytes);
var cancelBytes = new byte[QuestCancelled.Length];
_ = new QuestCancelled(cancelBytes) { QuestGroup = 18, QuestNumber = 0 };
journal.Apply(cancelBytes);
Check(journal.Active.Count == 0, "Cancellation matches group even when number is zero");
progressBytes[3] = 0x0C; journal.Apply(progressBytes);
var emptyList = new byte[QuestStateList.GetRequiredSize(0)];
_ = new QuestStateList(emptyList) { QuestCount = 0 };
journal.Apply(emptyList);
Check(journal.Active.Count == 0 && journal.Details.Count == 0, "Reconnection snapshot removes stale quests");
int revision = journal.Revision;
try { journal.Apply(progressBytes.AsSpan(0, 30)); throw new Exception("Malformed packet accepted"); }
catch (ArgumentException) { }
Check(journal.Revision == revision, "Truncated packets do not mutate state");
progressBytes[8] = 6;
try { journal.Apply(progressBytes); throw new Exception("Excess conditions accepted"); }
catch (ArgumentException) { }
Check(journal.Revision == revision, "Invalid counts do not mutate state");
var request = Client.Main.Core.Client.QuestJournal.Request(0x0B, 18, 0);
var accept = new MUnique.OpenMU.Network.Packets.ClientToServer.QuestProceedRequest(request);
Check(accept.QuestGroup == 18 && accept.QuestNumber == 0 && accept.ProceedAction == MUnique.OpenMU.Network.Packets.ClientToServer.QuestProceedRequest.QuestProceedAction.AcceptQuest, "Accept request interoperates with OpenMU generated packet");
journal.CloseNpc();
Check(journal.Npc == 0 && journal.Available.Count == 0 && journal.OfferedStep == null, "Closing NPC clears offers");
Check(QuestCatalog.Find(new(18, 1, true))?.Name == "Spider Hunt!", "S6 catalog resolves actual quest ID");
