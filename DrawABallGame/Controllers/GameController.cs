using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DrawABallGame.Repository;
using DrawABallGame.Models;



namespace DrawABallGame.Controllers
{

    public class GameController : ApiController
    {
        DrawBallDBEntities entities = new DrawBallDBEntities();
        DrawBallModel drawBallModel = new DrawBallModel();


        public IEnumerable<DrawGameStat> Get()
        {
            return entities.DrawGameStats.ToList();
        }


        /// <summary>
        /// Service  to Start Game and provide initial Values
        /// </summary>
        /// <param name="GameStats"></param>
        /// <returns> Values which have been updated</returns>
        [HttpPost]
        [Route("api/game/startgame")]
        public IHttpActionResult StartGame(DrawGameStat GameStats)
        {
            GameStats.GameID = Guid.NewGuid();
            GameStats.CreditAlotted = GameStats.RoundIterations * 20;
            GameStats.TotalBalls = 20;
            GameStats.DrawCost = 10;
            GameStats.SpentCredit = 0;
            GameStats.WonCredit = 0;
            GameStats.LeftRounds = GameStats.RoundIterations;
            GameStats.WinBonus = 20;
            GameStats.RTP = 0;
            GameStats.FreeDraw = "false";
            entities.DrawGameStats.Add(GameStats);
            entities.SaveChanges();

            drawBallModel.StartPlay(GameStats.RoundIterations);
            DrawBallModel.gameID = GameStats.GameID;
            return Ok<DrawGameStat>(GameStats);
        }


        [HttpGet]
        [Route("api/game/drawball")]
        public IHttpActionResult DrawBall()
        {
            return Ok<string>(drawBallModel.DrawBall(DrawBallModel.DrawType.Paid));
        }

        [HttpGet]
        public IHttpActionResult GetRTP()
        {
            var query = entities.DrawGameStats.SingleOrDefault(x => x.GameID == DrawBallModel.gameID);
            double val = 0.0;
            if (query != null)
            {
                val = query.RTP;
            }
            return Ok<double>(val);
        }

    }

}
