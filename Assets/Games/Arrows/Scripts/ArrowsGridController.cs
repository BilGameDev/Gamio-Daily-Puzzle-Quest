using System;

namespace Gamio.Games.Arrows
{
    public class ArrowsGridController : IDisposable
    {
        private readonly ArrowsPuzzle puzzle;
        private bool solved;
        private bool animating;

        public ArrowsPuzzle Puzzle => puzzle;
        public bool IsSolved => solved;
        public bool IsAnimating => animating;

        public event Action OnSolved;
        public event Action<int, int> OnTileRemoved;
        public event Action<int, int, int, int> OnTileBlocked;
        public event Action<int, int> OnTileRestored;
        public event Action OnPuzzleReset;

        public ArrowsGridController(ArrowsPuzzle puzzleData)
        {
            puzzle = puzzleData;
        }

        public bool TrySlideTile(int r, int c)
        {
            if (solved || animating) return false;
            if (!puzzle.HasTile(r, c)) return false;

            if (puzzle.CanSlide(r, c))
            {
                puzzle.RemoveTile(r, c);
                OnTileRemoved?.Invoke(r, c);

                if (puzzle.IsSolved())
                {
                    solved = true;
                    OnSolved?.Invoke();
                }
                return true;
            }

            var blocker = puzzle.FindBlocker(r, c);
            OnTileBlocked?.Invoke(r, c, blocker?.r ?? -1, blocker?.c ?? -1);
            return false;
        }

        public void NotifyAnimationComplete()
        {
            animating = false;
        }

        public void NotifyAnimationStarted()
        {
            animating = true;
        }

        public bool Undo()
        {
            if (animating) return false;
            if (puzzle.Undo(out int row, out int col))
            {
                solved = false;
                OnTileRestored?.Invoke(row, col);
                return true;
            }
            return false;
        }

        public void ResetPuzzle()
        {
            puzzle.Reset();
            solved = false;
            animating = false;
            OnPuzzleReset?.Invoke();
        }

        public void Dispose()
        {
            OnSolved = null;
            OnTileRemoved = null;
            OnTileBlocked = null;
            OnTileRestored = null;
            OnPuzzleReset = null;
        }
    }
}
