#region

using System;
using System.Collections;
using System.Collections.Generic;
using Game.Data;

#endregion

namespace Game.Map {
    class AllObjectEnum : IEnumerator {
        #region Members

        private bool isNew = true;
        private readonly ObjectList objectList;
        private Dictionary<int, List<SimpleGameObject>>.Enumerator itr;
        private List<SimpleGameObject>.Enumerator listItr;

        #endregion

        #region Constructors

        public AllObjectEnum(ObjectList objectList) {
            this.objectList = objectList;
            itr = this.objectList.dict.GetEnumerator();
        }

        #endregion

        #region IEnumerator Members

        public void Reset() {
            itr = objectList.dict.GetEnumerator();
        }

        public object Current {
            get { return listItr.Current; }
        }

        public bool MoveNext() {
            if (isNew) {
                isNew = false;
                if (itr.MoveNext()) {
                    listItr = itr.Current.Value.GetEnumerator();

                    return listItr.MoveNext();
                }

                return false;
            }

            if (listItr.MoveNext())
                return true;
            
            if (itr.MoveNext()) {
                listItr = itr.Current.Value.GetEnumerator();

                return listItr.MoveNext();
            }

            return false;
        }

        #endregion
    }

    public class ObjectList : IEnumerable {
        #region Members

        internal Dictionary<int, List<SimpleGameObject>> dict = new Dictionary<int, List<SimpleGameObject>>();

        #endregion

        #region Properties

        public ushort Count { get; private set; }

        #endregion

        #region Methods

        internal void AddGameObject(SimpleGameObject obj) {
            Add(obj, Region.GetTileIndex(obj.X, obj.Y));
        }

        private void Add(SimpleGameObject obj, int index) {
            List<SimpleGameObject> list;

            if (dict.TryGetValue(index, out list)) {
                if (list.Contains(obj))
                    throw new Exception("WTF");
                list.Add(obj);
            }
            else {
                list = new List<SimpleGameObject> {
                                                      obj
                                                  };
                dict[index] = list;
            }

            ++Count;
        }

        internal bool Remove(SimpleGameObject obj) {
            return Remove(obj, obj.X, obj.Y);
        }

        internal bool Remove(SimpleGameObject obj, uint origX, uint origY) {
            List<SimpleGameObject> list;
            int index = Region.GetTileIndex(origX, origY);

            if (dict.TryGetValue(index, out list)) {
                --Count;
                bool ret = list.Remove(obj);

                if (list.Count == 0)
                    dict.Remove(index); //Remove list if it is empty

                return ret;
            }

            return true;
        }

        internal List<SimpleGameObject> Get(uint x, uint y) {
            List<SimpleGameObject> list;

            int index = Region.GetTileIndex(x, y);

            if (dict.TryGetValue(index, out list))
                return new List<SimpleGameObject>(list);
            
            return new List<SimpleGameObject>();
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return new AllObjectEnum(this);
        }

        #endregion
    }
}