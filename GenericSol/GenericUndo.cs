using System;
using System.Collections.Generic;
using System.Text;

namespace GenericSol;


internal class GenericUndo
{
    internal GenericMove move;
    internal int FaceupPremove;
    public GenericUndo(GenericMove move, int faceupPremove = -1)
    {
        this.move = move;
        this.FaceupPremove = faceupPremove;
    }
}

