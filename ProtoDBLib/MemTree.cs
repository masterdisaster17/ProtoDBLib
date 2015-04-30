using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointProtoDB
{
    abstract internal class IMemTreeNode
    {
        private static bool _mergeDirection;
        protected static bool MergeDirection
        {
            get
            {
                _mergeDirection = !_mergeDirection;
                return _mergeDirection;
            }
        }

        public virtual int FirstBlock
        {
            get;
            set;
        }

        public virtual int LastBlock
        {
            get;
            set;
        }

        public int TotalBlocks
        {
            get { return (LastBlock - FirstBlock) + 1; }
        }

        public abstract IMemTreeNode Allocate(int blocks, out int offset);
        public abstract IMemTreeNode Free(int offset, int blocks);
        public abstract IMemTreeNode LeftLeaf
        {
            get;
        }
        public abstract IMemTreeNode RightLeaf
        {
            get;
        }

        public abstract IMemTreeNode SnipLeftLeaf();
        public abstract IMemTreeNode SnipRightLeaf();

        public abstract IMemTreeNode ExpandLeftLeaf(int blocks);
        public abstract IMemTreeNode ExpandRightLeaf(int blocks);

        public abstract int FreeBlocks
        {
            get;
        }

        public abstract int ContiguousBlocks
        {
            get;
        }

        public abstract int ContiguousBlocks_Raw
        {
            get;
        }

        public abstract int Height
        {
            get;
        }

        public abstract int Nodes
        {
            get;
        }

    }

    internal class MemTree
    {
        private IMemTreeNode root;

        public MemTree(int blocks)
        {
            root = new ContentNode() { FirstBlock = 0, LastBlock = blocks - 1 };
        }

        // returns the block at which the specifed number of blocks have been allocated
        // returns -1 if blocks could not be allocated
        public int Allocate(int blocks)
        {
            // Walk down the tree, stopping at the first node that can allocate the needed number of blocks
            int offset = -1;
            if (root != null)
            {
                root = root.Allocate(blocks, out offset);
            }
            return offset;
        }

        // frees the specified number of blocks at the given offset
        public void Free(int offset, int blocks)
        {
            // walk the tree, locating the node that holds the blocks to be freed
            if (root == null)
            {
                root = new ContentNode() { FirstBlock = offset, LastBlock = offset + blocks - 1 };
            }
            else
            {
                root = root.Free(offset, blocks);
            }
        }

        public int FreeBlocks
        {
            get
            {
                if (root != null)
                {
                    return root.FreeBlocks;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int ContiguousBlocks
        {
            get
            {
                if (root != null)
                {
                    return root.ContiguousBlocks;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int ContiguousBlocks_Raw
        {
            get
            {
                if (root != null)
                {
                    return root.ContiguousBlocks_Raw;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int Height
        {
            get
            {
                if (root != null)
                {
                    return root.Height;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int Nodes
        {
            get
            {
                if (root != null)
                {
                    return root.Nodes;
                }
                else
                {
                    return 0;
                }
            }
        }
    }

    internal class ContentNode : IMemTreeNode
    {
        public ContentNode()
        {

        }

        public override int FreeBlocks
        {
            get { return TotalBlocks; }
        }

        public override int ContiguousBlocks
        {
            get { return TotalBlocks; }
        }

        public override int ContiguousBlocks_Raw
        {
            get { return TotalBlocks; }
        }

        public override int Height
        {
            get { return 1; }
        }

        public override int Nodes
        {
            get { return 1; }
        }

        public override IMemTreeNode Allocate(int blocks, out int offset)
        {
            if (TotalBlocks > blocks)
            {   // we can allocate this here. Allocate from first available block, and shrink the contentnode
                offset = FirstBlock;
                FirstBlock += blocks;
                return this;
            }
            else if (TotalBlocks == blocks)
            {
                // we can allocate this here, but it will consume this entire node. Signal parent to realign
                offset = FirstBlock;
                return null;
            }
            else
            {   // we cannot allocate
                offset = -1;
                return this;
            }
        }

        public override IMemTreeNode Free(int offset, int blocks)
        {
            if (offset + blocks < FirstBlock)
            {   // we're freeing to the left of this contentnode, so turn it into an indexnode
                ContentNode newContentNode = new ContentNode() { FirstBlock = offset, LastBlock = offset + blocks - 1 };
                return new IndexNode(newContentNode, this);
            }
            else if (offset + blocks == FirstBlock)
            {   // we're freeing left adjacent to this contentnode, so simply expand it
                FirstBlock -= blocks;
                return this;
            }
            else if (offset - 1 == LastBlock)
            {   // we're freeing right adjacent to this contentnode, so simply expand it
                LastBlock += blocks;
                return this;
            }
            else if (offset - 1 > LastBlock)
            {   // We're freeing to the right of this contentnode, so turn it into an indexnode
                ContentNode newContentNode = new ContentNode() { FirstBlock = offset, LastBlock = offset + blocks - 1 };
                return new IndexNode(this, newContentNode);
            }

            throw new Exception("fuck this noise");
        }

        public override IMemTreeNode LeftLeaf
        {
            get { return this; }
        }
        public override IMemTreeNode RightLeaf
        {
            get { return this; }
        }

        public override IMemTreeNode SnipLeftLeaf()
        {
            return null;
        }
        public override IMemTreeNode SnipRightLeaf()
        {
            return null;
        }

        public override IMemTreeNode ExpandLeftLeaf(int blocks)
        {
            FirstBlock -= blocks;
            return this;
        }
        public override IMemTreeNode ExpandRightLeaf(int blocks)
        {
            LastBlock += blocks;
            return this;
        }

    }

    internal class IndexNode : IMemTreeNode
    {
        private IMemTreeNode leftChild;
        private IMemTreeNode rightChild;

        private int contiguousBlocks;

        public IndexNode(IMemTreeNode left, IMemTreeNode right)
        {
            leftChild = left;
            rightChild = right;
            contiguousBlocks = (left.ContiguousBlocks < right.ContiguousBlocks) ? right.ContiguousBlocks : left.ContiguousBlocks;
        }

        public override int FreeBlocks
        {
            get { return leftChild.FreeBlocks + rightChild.FreeBlocks; }
        }

        public override int ContiguousBlocks
        {
            get
            {
                return contiguousBlocks;
            }
        }

        public override int ContiguousBlocks_Raw
        {
            get { return (leftChild.ContiguousBlocks_Raw < rightChild.ContiguousBlocks_Raw) ? rightChild.ContiguousBlocks_Raw : leftChild.ContiguousBlocks_Raw; }
        }

        public override int Height
        {
            get
            {
                if (leftChild.Height > rightChild.Height)
                {
                    return leftChild.Height + 1;
                }
                else
                {
                    return rightChild.Height + 1;
                }
            }
        }

        public override int Nodes
        {
            get { return leftChild.Nodes + rightChild.Nodes + 1; }
        }

        public override int FirstBlock
        {
            get { return leftChild.FirstBlock; }
            set { leftChild.FirstBlock = value; }
        }

        public override int LastBlock
        {
            get { return rightChild.LastBlock; }
            set { rightChild.LastBlock = value; }
        }

        // get the leftmost leaf from the tree
        public override IMemTreeNode LeftLeaf
        {
            get { return leftChild.LeftLeaf; }
        }
        // get the rightmost tree from the tree
        public override IMemTreeNode RightLeaf
        {
            get { return rightChild.RightLeaf; }
        }

        // removes the leftmost leaf from the tree
        public override IMemTreeNode SnipLeftLeaf()
        {
            IMemTreeNode newLeftChild = leftChild.SnipLeftLeaf();
            if (newLeftChild == null)
            {   // leftChild is a leaf node
                return rightChild;
            }
            else
            {
                leftChild = newLeftChild;
                contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                return this;
            }
        }
        // removes the rightmost leaf from the tree
        public override IMemTreeNode SnipRightLeaf()
        {
            IMemTreeNode newRightChild = rightChild.SnipRightLeaf();
            if (newRightChild == null)
            {   // rightChild is a leaf node
                return leftChild;
            }
            else
            {
                rightChild = newRightChild;
                contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                return this;
            }
        }

        public override IMemTreeNode ExpandLeftLeaf(int blocks)
        {
            leftChild.ExpandLeftLeaf(blocks);
            contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
            return this;
        }
        public override IMemTreeNode ExpandRightLeaf(int blocks)
        {
            rightChild.ExpandRightLeaf(blocks);
            contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
            return this;
        }

        public override IMemTreeNode Allocate(int blocks, out int offset)
        {
            // locate a child node that can allocate blocks
            int leftDelta, rightDelta;
            leftDelta = leftChild.ContiguousBlocks - blocks;
            rightDelta = rightChild.ContiguousBlocks - blocks;

            if (leftDelta < 0 && rightDelta < 0)
            {
                offset = -1;
                return this;
            }

            if (leftDelta < 0)
            {
                // left delta is negative, so make sure it loses out in the next if statement
                leftDelta = rightDelta + 1;
            }
            else if (rightDelta < 0)
            {
                // right delta is negative, so make sure it loses out in the next if statement
                rightDelta = leftDelta + 1;
            }

            // check which side has a contiguous block closest to the size we need
            if (leftDelta < rightDelta)
            {
                // note that this CAN'T fail and return -1; We've just checked that there IS a contiguous block that will hold the allocation
                leftChild = leftChild.Allocate(blocks, out offset);
                if (leftChild == null)
                {
                    // left child was consumed by allocation
                    return rightChild;
                }
                else
                {
                    // This index nodes will continue to exist, so update the contiguous block count
                    // THIS can be optimized. Taking into account that we know the contiguous count for the subnodes before the allocation, and how many blocks we'll be shaving off
                    // it is possible to determine wether we actually need to do this update or not.
                    contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                    return this;
                }
            }
            else
            {
                // note that this CAN'T fail and return -1; We've just checked that there IS a contiguous block that will hold the allocation
                rightChild = rightChild.Allocate(blocks, out offset);
                if (rightChild == null)
                {
                    // right child was consumed by allocation
                    return leftChild;
                }
                else
                {
                    // This index nodes will continue to exist, so update the contiguous block count
                    // THIS can be optimized. Taking into account that we know the contiguous count for the subnodes before the allocation, and how many blocks we'll be shaving off
                    // it is possible to determine wether we actually need to do this update or not.
                    contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                    return this;
                }
            }


            //leftChild = leftChild.Allocate(blocks, out offset);
            //if (offset == -1)
            //{

            //    rightChild = rightChild.Allocate(blocks, out offset);
            //    if (rightChild == null)
            //    {   // right child was consumed by allocation
            //        return leftChild;
            //    }
            //    else
            //    {
            //        // This index nodes will continue to exist, so update the contiguous block count
            //        // THIS can be optimized. Taking into account that we know the contiguous count for the subnodes before the allocation, and how many blocks we'll be shaving off
            //        // it is possible to determine wether we actually need to do this update or not.
            //        contiguousBlocks = (contiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : contiguousBlocks;
            //        return this;
            //    }
            //}
            //else if (leftChild == null)
            //{   // left child was consumed by allocation
            //    return rightChild;
            //}
            //else
            //{
            //    // This index nodes will continue to exist, so update the contiguous block count
            //    // THIS can be optimized. Taking into account that we know the contiguous count for the subnodes before the allocation, and how many blocks we'll be shaving off
            //    // it is possible to determine wether we actually need to do this update or not.
            //    contiguousBlocks = (leftChild.ContiguousBlocks < contiguousBlocks) ? contiguousBlocks : leftChild.ContiguousBlocks;
            //    return this;
            //}
        }

        public override IMemTreeNode Free(int offset, int blocks)
        {
            if (leftChild.LastBlock > offset) // is this offset contained in the left subtree? 
            {
                leftChild = leftChild.Free(offset, blocks);
                contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;

                if (leftChild.LastBlock + 1 == rightChild.FirstBlock)
                {   // left and right nodes have become contiguous, so merge them
                    return Merge();
                }

                return this;
            }
            else if (rightChild.FirstBlock < offset) //is this offset contained in the right subtree?
            {
                rightChild = rightChild.Free(offset, blocks);
                contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;

                if (leftChild.LastBlock + 1 == rightChild.FirstBlock)
                {   // left and right nodes have become contiguous, so merge them
                    return Merge();
                }

                return this;
            }
            else
            {
                // oh crap, not in either of them. Guess we'll have to handle this ourselves.
                // does the block to free lie adjacent ot the leftchild or rightchild?
                bool leftAdjacent, rightAdjacent;
                leftAdjacent = (offset - 1 == leftChild.LastBlock);
                rightAdjacent = (offset + blocks == rightChild.FirstBlock);

                if (leftAdjacent)
                {
                    if (rightAdjacent)
                    {   // Adjacent to both right and left, so merge them into one.
                        return Merge();
                    }
                    else
                    {   // Adjacent to left only, expand left
                        leftChild.ExpandRightLeaf(blocks);
                        contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                        return this;
                    }
                }
                else if (rightAdjacent)
                {   // adjacent to right only, expand right;
                    rightChild.ExpandLeftLeaf(blocks);
                    contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                    return this;
                }
                else
                {
                    // adjacent to nothing, create new contentnode for the freed space, and a new index node to contain it.
                    ContentNode newContentNode = new ContentNode() { FirstBlock = offset, LastBlock = offset + blocks - 1 };
                    IndexNode newIndexNode = null;
                    if (IMemTreeNode.MergeDirection)
                    {
                        newIndexNode = new IndexNode(newContentNode, rightChild); // merging it into right subtree
                        return new IndexNode(leftChild, newIndexNode);
                    }
                    else
                    {
                        newIndexNode = new IndexNode(leftChild, newContentNode); // merging it into left subtree
                        return new IndexNode(newIndexNode, rightChild);
                    }

                }
            }
        }

        private IMemTreeNode Merge()
        {
            if (rightChild is ContentNode)
            {
                if (leftChild is ContentNode)
                {
                    // if they're both contentnodes, we can just substitute them with a single new contentnode
                    return new ContentNode() { FirstBlock = leftChild.FirstBlock, LastBlock = rightChild.LastBlock };
                }
                else
                {   // right is a contentnode, but left isn't
                    // merge the right contentnode into the lefttree
                    leftChild.ExpandRightLeaf(rightChild.LastBlock - leftChild.RightLeaf.LastBlock);
                    return leftChild;
                }
            }
            else if (leftChild is ContentNode)
            {   // left is a contentnode, but right isn't
                // merge the left contentnode into the righttree
                rightChild.ExpandLeftLeaf(rightChild.LeftLeaf.FirstBlock - leftChild.FirstBlock);
                return rightChild;
            }
            else
            {   // both left and right are index nodes. fuck it.
                // snip the leftmost leaf off the right tree
                IMemTreeNode snipped = rightChild.LeftLeaf;
                rightChild = rightChild.SnipLeftLeaf();
                // append that to the rightmost leaf of the left tree
                leftChild.RightLeaf.LastBlock = snipped.LastBlock;
                contiguousBlocks = (leftChild.ContiguousBlocks < rightChild.ContiguousBlocks) ? rightChild.ContiguousBlocks : leftChild.ContiguousBlocks;
                return this;
            }
        }

    }
}
