﻿using System;

namespace Library.Functional {
    /// <summary>
    ///     Functional data data to represent a discriminated
    ///     union of two possible types.
    /// </summary>
    /// <typeparam name="TL">Type of "Left" item.</typeparam>
    /// <typeparam name="TR">Type of "Right" item.</typeparam>
    public class Either<TL, TR> {
        public readonly bool isLeft;
        public bool isRight => !isLeft;
        readonly TL left;
        readonly TR right;

        public Either(TL left) {
            this.left = left;
            isLeft = true;
        }

        public Either(TR right) {
            this.right = right;
            isLeft = false;
        }

        public T Match<T>(Func<TL, T> leftFunc, Func<TR, T> rightFunc) {
            if (leftFunc == null) throw new ArgumentNullException(nameof(leftFunc));

            if (rightFunc == null) throw new ArgumentNullException(nameof(rightFunc));

            return isLeft ? leftFunc(left) : rightFunc(right);
        }

        public void MatchAction(Action<TL> leftAction, Action<TR> rightAction) {
            if (leftAction == null) throw new ArgumentNullException(nameof(leftAction));

            if (rightAction == null) throw new ArgumentNullException(nameof(rightAction));

            if (isLeft) {
                leftAction(left);
            }
            else {
                rightAction(right);
            }
        }

        /// <summary>
        ///     If right value is assigned, execute an action on it.
        /// </summary>
        /// <param name="rightAction">Action to execute.</param>
        public void DoRight(Action<TR> rightAction) {
            if (rightAction == null) throw new ArgumentNullException(nameof(rightAction));

            if (!isLeft) rightAction(right);
        }
        
        public TL LeftOrDefault => Match(l => l, r => default);

        public TR RightOrDefault => Match(l => default, r => r);

        public static implicit operator Either<TL, TR>(TL left) {
            return new Either<TL, TR>(left);
        }

        public static implicit operator Either<TL, TR>(TR right) {
            return new Either<TL, TR>(right);
        }
    }
}