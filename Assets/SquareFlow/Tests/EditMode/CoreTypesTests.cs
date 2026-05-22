using NUnit.Framework;
using SquareFlow.Core;

namespace SquareFlow.Tests
{
    public sealed class CoreTypesTests
    {
        [Test]
        public void BoardCellFactoryCreatesNormalBombAndEmptyCells()
        {
            BoardCell normal = BoardCell.Normal(FlowColor.Red, 3);
            BoardCell bomb = BoardCell.Bomb();
            BoardCell empty = BoardCell.Empty;

            Assert.That(normal.Type, Is.EqualTo(BoardCellType.Normal));
            Assert.That(normal.Color, Is.EqualTo(FlowColor.Red));
            Assert.That(normal.Hp, Is.EqualTo(3));
            Assert.That(bomb.Type, Is.EqualTo(BoardCellType.Bomb));
            Assert.That(bomb.Hp, Is.EqualTo(1));
            Assert.That(empty.Type, Is.EqualTo(BoardCellType.Empty));
            Assert.That(empty.IsOccupied, Is.False);
        }

        [Test]
        public void ShooterStoresColorAmmoWildAndHiddenFlags()
        {
            Shooter shooter = new Shooter("s1", FlowColor.Wild, 7, true, true);

            Assert.That(shooter.Id, Is.EqualTo("s1"));
            Assert.That(shooter.Color, Is.EqualTo(FlowColor.Wild));
            Assert.That(shooter.Ammo, Is.EqualTo(7));
            Assert.That(shooter.Wild, Is.True);
            Assert.That(shooter.Hidden, Is.True);
        }

        [Test]
        public void ConstantsMatchSourceGameLimits()
        {
            Assert.That(SquareFlowConstants.Speed, Is.EqualTo(320f));
            Assert.That(SquareFlowConstants.WaitQueueLimit, Is.EqualTo(5));
            Assert.That(SquareFlowConstants.MaxActiveOrbiters, Is.EqualTo(5));
            Assert.That(SquareFlowConstants.NormalColorCount, Is.EqualTo(4));
            Assert.That(SquareFlowConstants.ExtraShooterCount, Is.EqualTo(6));
            Assert.That(SquareFlowConstants.ComboResetSeconds, Is.EqualTo(1.3f));
        }
    }
}
