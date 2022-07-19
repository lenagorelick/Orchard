using System;
using UnityEngine;

namespace Orchard
{
    public interface FruitContainer
    {
        public void SpawnFruit();
        public void RemoveFruit(Fruit fruit);
        public void Reach(Vector3 target);
        public void Unreach(bool detachHappened);
    }
}