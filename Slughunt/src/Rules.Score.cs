using System.IO;
using Slughunt.Utils;

namespace Slughunt;

public static partial class Rules {
    public sealed record Score(Role.Participant role) {
        public OnlineTimeSpan time { get; set; }

        public uint caught { get; private set; }
        public uint killCaught { get; private set; }
        public uint oppositeKilled { get; private set; }
        public uint teamKilled { get; private set; }
        public uint otherDeaths { get; private set; }
        public uint oppositeKills { get; private set; }
        public uint teamKills { get; private set; }

        public long total => role.TotalScore(this);

        public void Write(BinaryWriter writer) {
            time.Write(writer);
            writer.Write(caught);
            writer.Write(killCaught);
            writer.Write(oppositeKilled);
            writer.Write(teamKilled);
            writer.Write(otherDeaths);
            writer.Write(oppositeKills);
            writer.Write(teamKills);
        }

        public void Read(BinaryReader reader) {
            time = OnlineTimeSpan.Read(reader);
            caught = reader.ReadUInt32();
            killCaught = reader.ReadUInt32();
            oppositeKilled = reader.ReadUInt32();
            teamKilled = reader.ReadUInt32();
            otherDeaths = reader.ReadUInt32();
            oppositeKills = reader.ReadUInt32();
            teamKills = reader.ReadUInt32();
        }

        public void ReadTo(Score other) {
            other.time = time;
            other.caught = caught;
            other.killCaught = killCaught;
            other.oppositeKilled = oppositeKilled;
            other.teamKilled = teamKilled;
            other.otherDeaths = otherDeaths;
            other.oppositeKills = oppositeKills;
            other.teamKills = teamKills;
        }

        public static void ScoreCatchOrKill(PlayerData attacker, PlayerData victim, bool isCatch, bool kill) {
            kill = kill && !victim.dead;
            if (isCatch) {
                if (kill) {
                    attacker.score.killCaught++;
                    victim.score.killCaught++;
                }
                else {
                    attacker.score.caught++;
                    victim.score.caught++;
                }
            }
            else if (kill) {
                if (attacker.role == victim.role) {
                    attacker.score.teamKills++;
                    victim.score.teamKilled++;
                }
                else {
                    attacker.score.oppositeKills++;
                    victim.score.oppositeKilled++;
                }
            }
        }

        public static void ScoreDeath(PlayerData player) {
            if (!player.dead)
                player.score.otherDeaths++;
        }
    }
}
