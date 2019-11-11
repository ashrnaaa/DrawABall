using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DrawABallGame.Repository;
using DrawABallGame.Controllers;

/*Implement a game with the following features:
There are 20 balls in a basket, for being able to pick a ball you need to pay 10 credits.
Each ball will give you a ”win”, ”extra pick” or ”no win” (in case of extra pick, you will draw
another ball with no cost).
From these 20 balls, 5 of them will give you 20 credits(”win” type), 1 of them will give you extra
pick(”extra pick” type) and 14 of them no win(”no win” type).
After each pick you will place the ball back to the basket.After each win the balance of the
player will get updated with the win amount.
You should be able to run/simulate your game with a specified number of rounds* via a player
with unlimited credits and then calculate the RTP(return to player).
RTP = ((number of won credits) / (number of credits that are spent to play the game))*100.

 * * One round contains the event (s) that you pay for the current picked ball till(not including) the
 next paid picked ball.For example, you pay for a pick and after the pick the ball turns out to be
an ”extra pick” ball(first event), then you place the ball back and pick another one for free and
turns out that ball is a ”no win” ball(second event), these two events/picks are considered as
one game round.Following scenario is also a game round with one event/pick: Pay for a pick
and pick a ball, then the picked ball is a ”win” ball → update the balance.*/


namespace DrawABallGame.Models
{
    public class DrawBallModel
    {

        DrawBallDBEntities entities = new DrawBallDBEntities();
        //GameController gameController = new GameController();
        public enum BallType
        {
            Win,
            NoWin,
            ExtraPick
        }

        public BallType drawnBall = BallType.NoWin;

        public enum DrawType
        {
            Paid,
            Free
        }
        public static Guid gameID;
        public double ballCount = 20; //total number of balls in the bucket at a time
        public int roundCount = 0, leftRounds = 0;      // number of rounds for the present game, to be set by the user when game starts
        public int drawCost = 10;
        public int winBonus = 20;
        public int noWinBalls = 5, noExtraPick = 1, noNoWin = 14;
        public double alottedCredit = 0;
        public double probabilityWin, probabilityNoWin, probabilityExtraPick;
        public bool isFreeDraw = false;

        public double RTP = 0, spentCredit = 0, wonCredit = 0;

        public List<KeyValuePair<BallType, double>> probabilityElements = new List<KeyValuePair<BallType, double>>();
        public List<DrawGameStat> GameStatList = new List<DrawGameStat>();
        public DrawGameStat requiredGameStat;

        public List<DrawGameStat> GetData()
        {
            return entities.DrawGameStats.ToList();
        }


        public void SetStats()
        {
            GameStatList = GetData();
            foreach (var item in GameStatList)
            {
                if (item.GameID == gameID)
                {
                    requiredGameStat = item;
                    leftRounds = item.LeftRounds;
                    alottedCredit = item.CreditAlotted;
                    spentCredit = item.SpentCredit;
                    wonCredit = item.WonCredit;
                    isFreeDraw = bool.Parse(item.FreeDraw);
                }

            }
        }
        public void StartPlay(int noOfRounds)
        {
            roundCount = leftRounds = noOfRounds;
            alottedCredit = noOfRounds * 20;
        }

        public void OnWin()
        {
            alottedCredit += winBonus;
            wonCredit += winBonus;
            var query = entities.DrawGameStats.SingleOrDefault(x => x.GameID == gameID);
            if (query != null)
            {
                query.CreditAlotted = alottedCredit;
                query.WonCredit = wonCredit;
                entities.SaveChanges();
            }
        }

        public void OnNoWin()
        {

        }

        public void OnExtraDraw()
        {
            isFreeDraw = true;
            var query = entities.DrawGameStats.SingleOrDefault(x => x.GameID == gameID);
            if (query != null)
            {
                query.FreeDraw = isFreeDraw.ToString();
                entities.SaveChanges();
            }
        }

        public string OnGameOver()
        {
            RTP = (wonCredit / spentCredit) * 100;
            var query = entities.DrawGameStats.SingleOrDefault(x => x.GameID == gameID);
            if (query != null)
            {
                query.RTP = RTP;
                entities.SaveChanges();
            }
            return RTP.ToString();
        }

        public string DrawBall(DrawType drawType)
        {
            SetStats();
            if(isFreeDraw == true)
            {
                drawType = DrawType.Free;
            }

            if (drawType == DrawType.Paid)
            {
                if (alottedCredit > drawCost && leftRounds > 0)
                {
                    alottedCredit -= drawCost;
                    spentCredit += drawCost;
                    leftRounds -= 1;
                    drawnBall = CalculateProbablityNextBall();

                    var query = entities.DrawGameStats.SingleOrDefault(x => x.GameID == gameID);
                    if (query != null)
                    {
                        query.CreditAlotted = alottedCredit;
                        query.SpentCredit = spentCredit;
                        query.LeftRounds = leftRounds;
                        entities.SaveChanges();
                    }
                    CheckDrawResult(drawnBall);
                    return drawnBall.ToString();
                }
                else
                {
                    return OnGameOver();
                }

            }
            else
            {
                isFreeDraw = false;
                var query = entities.DrawGameStats.SingleOrDefault(x => x.GameID == gameID);
                if (query != null)
                {
                    query.FreeDraw = isFreeDraw.ToString();
                    entities.SaveChanges();
                }
                drawnBall = CalculateProbablityNextBall();
                CheckDrawResult(drawnBall);
                return drawnBall.ToString();
            }

        }

        public void CheckDrawResult(BallType ballType)
        {
            if (drawnBall == BallType.ExtraPick)
            {
                OnExtraDraw();
            }
            else if (drawnBall == BallType.NoWin)
            {
                OnNoWin();
            }
            else
            {
                OnWin();
            }
        }

        public BallType CalculateProbablityNextBall()
        {
            BallType nextBallType = BallType.NoWin;
            probabilityExtraPick = noExtraPick / ballCount;
            probabilityNoWin = noNoWin / ballCount;
            probabilityWin = noWinBalls / ballCount;


            double cumulativeProbability = probabilityExtraPick;
            probabilityElements.Add(new KeyValuePair<BallType, double>(BallType.ExtraPick, cumulativeProbability));
            cumulativeProbability += probabilityWin;
            probabilityElements.Add(new KeyValuePair<BallType, double>(BallType.Win, cumulativeProbability));
            cumulativeProbability += probabilityNoWin;
            probabilityElements.Add(new KeyValuePair<BallType, double>(BallType.NoWin, cumulativeProbability));

            Random range = new Random();
            double value = range.NextDouble();
            Console.WriteLine(value);

            double cumulative = 0.0;
            for (int i = 0; i < probabilityElements.Count; i++)
            {
                cumulative += probabilityElements[i].Value;
                if (value < cumulative)
                {
                    string selectedElement = probabilityElements[i].Key.ToString();
                    nextBallType = probabilityElements[i].Key;
                    Console.WriteLine(selectedElement.ToString());
                    break;
                }
            }
            return nextBallType;
        }
    }
}