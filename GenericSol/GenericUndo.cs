using System;
using System.Collections.Generic;
using System.Text;

namespace GenericSol;


internal class GenericUndo
{
    internal GenericMove move;
    internal int FaceupPremove;
    internal string State;

    public GenericUndo(GenericMove move, int faceupPremove = -1, string state = "")
    {
        this.move = move;
        this.FaceupPremove = faceupPremove;
        this.State = state;
    }
}

