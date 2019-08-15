using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkersBoard : MonoBehaviour
{
  public Piece[,] pieces = new Piece[8, 8];
  public GameObject whitePiecePrefab;
  public GameObject blackPiecePrefab;

  private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
  private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

  public bool isWhite;
  private bool isWhiteTurn;
  private bool hasKilled;

  private Piece selectedPiece;
  private List<Piece> forcedPieces;

  private Vector2 mouseOver;
  private Vector2 startPosition;
  private Vector2 endPosition;

  private void Start()
  {
    isWhiteTurn = true;
    forcedPieces = new List<Piece>();
    generateBoard();
  }

  private void Update()
  {
    UpdateMouseOver();

    // if it is my turn
    if (true) {
      int x = (int)mouseOver.x;
      int y = (int)mouseOver.y;

      if (selectedPiece != null) {
        UpdatePieceDrag(selectedPiece);
      }

      if (Input.GetMouseButtonDown(0)) {
        SelectPiece(x, y);
      }

      if (Input.GetMouseButtonUp(0)) {
        TryMove((int)startPosition.x, (int)startPosition.y, x, y);
      }
    }
  }

  private void UpdateMouseOver()
  {
    if (!Camera.main) {
      Debug.Log("Unable to find main camera.");
      return;
    }

    RaycastHit hit;
    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
    {
      mouseOver.x = (int)(hit.point.x - boardOffset.x);
      mouseOver.y = (int)(hit.point.z - boardOffset.z);
    } else {
      mouseOver.x = -1;
      mouseOver.y = -1;
    }
  }

  private void UpdatePieceDrag(Piece p)
  {
    if (!Camera.main) {
      Debug.Log("Unable to find main camera.");
      return;
    }

    RaycastHit hit;
    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
    {
      p.transform.position = hit.point + Vector3.up;
    }
  }

  private void SelectPiece(int x, int y)
  {
    // if out of bounds
    if (x < 0 || x >= 8 || y < 0 || y >= 8) {
      return;
    }

    Piece p = pieces[x, y];
    if (p != null && p.isWhite == isWhite) {
      if (forcedPieces.Count == 0) {
        selectedPiece = p;
        startPosition = mouseOver;
      } else {
        // look for the piece under out forced pieces list
        if (forcedPieces.Find(fp => fp == p) == null) {
          return;
        }
        selectedPiece = p;
        startPosition = mouseOver;
      }
    }
  }

  private void TryMove(int x1, int y1, int x2, int y2)
  {
    forcedPieces = ScanForPossibleMove();

    // multiplayer support
    startPosition = new Vector2(x1, y1);
    endPosition = new Vector2(x2, y2);
    selectedPiece = pieces[x1, y1];

    if (x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
    {
      if (selectedPiece != null) {
        MovePiece(selectedPiece, x1, y1);
      }

      startPosition = Vector2.zero;
      selectedPiece = null;
      return;
    }

    // If it has not been moved
    if (selectedPiece != null) {
      if (endPosition == startPosition) {
        MovePiece(selectedPiece, x1, y1);
        startPosition = Vector2.zero;
        selectedPiece = null;
        return;
      }
    }

    // Check if it is valid
    if (selectedPiece.ValidMove(pieces, x1, y1, x2, y2)) {
      // did we kill anything
      // if this is a jump
      if (Mathf.Abs(x2 - x1) == 2) {
        Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
        if (p != null) {
          pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
          Destroy(p.gameObject);
          hasKilled = true;
        }
      }

      // were we supposed to kill anything
      if (forcedPieces.Count != 0 && !hasKilled) {
        MovePiece(selectedPiece, x1, y1);
        startPosition = Vector2.zero;
        selectedPiece = null;
        return;
      }

      pieces[x2, y2] = selectedPiece;
      pieces[x1, y1] = null;
      MovePiece(selectedPiece, x2, y2);

      EndTurn();
    } else {
      MovePiece(selectedPiece, x1, y1);
      startPosition = Vector2.zero;
      selectedPiece = null;
      return;
    }
  }

  private void EndTurn()
  {
    selectedPiece = null;
    startPosition = Vector2.zero;

    isWhiteTurn = !isWhiteTurn;
    hasKilled = false;
    CheckVictory();
  }

  private void CheckVictory()
  {

  }

  private List<Piece> ScanForPossibleMove()
  {
    forcedPieces = new List<Piece>();

    // check all the pieces
    for (int i = 0; i < 8; i++) {
      for (int j = 0; j < 8; j++) {
        if (pieces[i, j] != null && pieces[i, j].isWhite == isWhiteTurn) {
          if (pieces[i, j].IsForcedToMove(pieces, i, j)) {
            forcedPieces.Add(pieces[i, j]);
          }
        }
      }
    }

    return forcedPieces;
  }

  private void generateBoard()
  {
    // generate white team
    for (int y = 0; y < 3; y++)
    {
      bool oddRow = (y % 2 == 0);
      for (int x = 0; x < 8; x += 2)
      {
        GeneratePiece((oddRow) ? x : x + 1, y);
      }
    }
    // generate black team
    for (int y = 7; y > 4; y--)
    {
      bool oddRow = (y % 2 == 0);
      for (int x = 0; x < 8; x += 2)
      {
        GeneratePiece((oddRow) ? x : x + 1, y);
      }
    }
  }

  private void GeneratePiece(int x, int y)
  {
    bool isPieceWhite = (y > 3) ? false : true;
    GameObject gameObject = Instantiate((isPieceWhite) ? whitePiecePrefab : blackPiecePrefab) as GameObject;
    gameObject.transform.SetParent(transform);
    Piece p = gameObject.GetComponent<Piece>();
    pieces[x, y] = p;
    MovePiece(p, x, y);
  }

  private void MovePiece(Piece p, int x, int y)
  {
    p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
  }
}
