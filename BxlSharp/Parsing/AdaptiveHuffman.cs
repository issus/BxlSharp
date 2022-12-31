using System.Text;

namespace OriginalCircuit.BxlSharp
{
    /// <summary>
    /// Performs the decoding of data stored using adaptive huffman coding.
    /// Source: http://dangerousprototypes.com/blog/2012/05/07/how-to-generate-footprints-for-microships-parts/#comment-282598
    /// </summary>
    public static class AdaptiveHuffman
    {

        private static Node CreateTree()
        {
            // create root node
            var root = new Node(null);

            // fill levels
            var node = root;
            int leafCount = 0;
            while (node != null)
            {
                node = root.AddChild((char)leafCount);
                if (node?.IsLeaf == true)
                {
                    leafCount++;
                }
            }
            return root;
        }

        private static int UncompressedSize(byte[] buffer)
        {
            /* Uncompressed size =
               B0b7 * 1<<0 + B0b6 * 1<<1 + ... + B0b0 * 1<<7 +
               B1b7 * 1<<0 + B1b6 * 1<<1 + ... + B2b0 * 1<<7 +
               B2b7 * 1<<0 + B2b6 * 1<<1 + ... + B3b0 * 1<<7 +
               B3b7 * 1<<0 + B3b6 * 1<<1 + ... + B4b0 * 1<<7
            */
            int size = 0;
            int mask = 0;
            for (int i = 7; i >= 0; i--)
            {
                if ((buffer[0] & (1 << i)) != 0)
                {
                    size |= (1 << mask);
                }
                mask++;
            }
            for (int i = 7; i >= 0; i--)
            {
                if ((buffer[1] & (1 << i)) != 0)
                {
                    size |= (1 << mask);
                }
                mask++;
            }
            for (int i = 7; i >= 0; i--)
            {
                if ((buffer[2] & (1 << i)) != 0)
                {
                    size |= (1 << mask);
                }
                mask++;
            }
            for (int i = 7; i >= 0; i--)
            {
                if ((buffer[3] & (1 << i)) != 0)
                {
                    size |= (1 << mask);
                }
                mask++;
            }
            return size;
        }

        private static int GetNextBit(byte[] buffer, ref int bufferIndex, ref byte currentByte, ref int currentBit)
        {
            if (currentBit < 0)
            {
                // Fetch next byte from source_buffer
                currentBit = 7;
                currentByte = buffer[bufferIndex++];
            }
            return currentByte & (1 << currentBit--);
        }

        public static string Decode(byte[] buffer)
        {
            var root = CreateTree();
            byte currentByte = default;
            var currentBit = 0;
            var bufferIndex = 4;
            
            var size = UncompressedSize(buffer);
            var sb = new StringBuilder(size);
            while (bufferIndex < buffer.Length && sb.Length != size)
            {
                var node = root;
                while (!node.IsLeaf)
                {
                    // find leaf node
                    if (GetNextBit(buffer, ref bufferIndex, ref currentByte, ref currentBit) != 0)
                    {
                        node = node.Left;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
                sb.Append(node.Symbol);
                node.Weight += 1;
                node.UpdateTree();
            }
            return sb.ToString();
        }
    }

    internal class Node
    {
        private Node _parent;
        internal readonly char Symbol;
        private readonly int _level;
        public readonly bool IsLeaf;
        public Node Left;
        public Node Right;
        public int Weight;

        public Node(Node parent, char symbol = default)
        {
            _parent = parent;
            if (_parent != null)
            {
                _level = _parent._level + 1;
                IsLeaf = _level > 7;
                if (IsLeaf)
                {
                    Symbol = symbol;
                }
            }
        }

        public Node AddChild(char symbol)
        {
            if (_level < 7)
            {
                if (Right != null)
                {
                    var ret = Right.AddChild(symbol);
                    if (ret != null) return ret;
                }
                if (Left != null)
                {
                    var ret = Left.AddChild(symbol);
                    if (ret != null) return ret;
                }
                if (Right == null) // first fill right branch
                {
                    Right = new Node(this);
                    return Right;
                }
                if (Left == null)
                {
                    Left = new Node(this);
                    return Left;
                }
                return null;
            }
            else
            {
                if (Right == null)
                {
                    Right = new Node(this, symbol);
                    return Right;
                }
                else if (Left == null)
                {
                    Left = new Node(this, symbol);
                    return Left;
                }
                else
                {
                    return null; // Leaves are filled
                }
            }
        }

        private Node Sibling(Node node)
        {
            if (node != Right)
            {
                return Right;
            }
            else
            {
                return Left;
            }
        }

        private bool NeedsSwapping()
        {
            if (_parent != null && _parent._parent != null && // root node
                Weight > _parent.Weight)
            {
                return true;
            }
            return false;
        }

        private static void Swap(Node n1, Node n2, Node n3)
        {
            if (n3 != null)
            {
                n3._parent = n1;
            }

            if (n1.Right == n2)
            {
                n1.Right = n3;
            }
            else if (n1.Left == n2)
            {
                n1.Left = n3;
            }
        }

        internal void UpdateTree()
        {
            while (NeedsSwapping())
            {
                var parent = _parent;
                var grandParent = parent._parent;
                var parentSibling = grandParent.Sibling(parent);
                Swap(grandParent, parent, this);
                Swap(grandParent, parentSibling, parent);
                Swap(parent, this, parentSibling);
                parent.Weight = parent.Right.Weight + parent.Left.Weight;
                grandParent.Weight = Weight + parent.Weight;

                parent.UpdateTree();
                grandParent.UpdateTree();
            }
        }
    }
}
