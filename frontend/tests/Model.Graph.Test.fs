module ModelGraphTests

open Fable.Jester
open Model
open Domain

Jest.describe (
  "Model.Update (Graph)",
  fun () ->
    Jest.test (
      "GraphLoaded success updates graph",
      fun () ->
        let initialState = State.Default

        let testGraph = {
          Nodes = [ "note1"; "note2"; "note3" ]
          Edges = [
            { Source = "note1"; Target = "note2"; Type = "wiki" }
            { Source = "note2"; Target = "note3"; Type = "wiki" }
          ]
        }

        let newState, _ = Update (GraphLoaded(Ok testGraph)) initialState
        Jest.expect(newState.Graph.IsSome).toEqual true
        Jest.expect(newState.Loading).toEqual false
        Jest.expect(newState.Error).toEqual None
    )

    Jest.test (
      "GraphLoaded error sets error message",
      fun () ->
        let initialState = State.Default
        let errorMsg = "Failed to load graph"
        let newState, _ = Update (GraphLoaded(Error errorMsg)) initialState
        Jest.expect(newState.Error).toEqual (Some errorMsg)
        Jest.expect(newState.Loading).toEqual (false)
    )

    Jest.test (
      "GraphNodeHovered updates hovered node",
      fun () ->
        let initialState = State.Default
        let nodeId = "note1"
        let newState, _ = Update (GraphNodeHovered(Some nodeId)) initialState
        Jest.expect(newState.HoveredNode).toEqual (Some nodeId)
    )

    Jest.test (
      "GraphZoomChanged updates zoom state",
      fun () ->
        let initialState = State.Default
        let newZoomState = { Scale = 1.5; TranslateX = 10.0; TranslateY = 20.0 }

        let newState, _ = Update (GraphZoomChanged newZoomState) initialState
        Jest.expect(newState.ZoomState.Scale).toEqual (1.5)
        Jest.expect(newState.ZoomState.TranslateX).toEqual (10.0)
        Jest.expect(newState.ZoomState.TranslateY).toEqual (20.0)
    )

    Jest.test (
      "GraphEngineChanged updates graph engine",
      fun () ->
        let initialState = State.Default
        let newState, _ = Update (GraphEngineChanged Canvas) initialState
        Jest.expect(newState.GraphEngine).toEqual (Canvas)
    )

    Jest.test (
      "GraphNodeHovered clears hover when None",
      fun () ->
        let initialState = { State.Default with HoveredNode = Some "note1" }
        let newState, _ = Update (GraphNodeHovered None) initialState
        Jest.expect(newState.HoveredNode).toEqual (None)
    )
)

Jest.describe (
  "GraphView.buildNeighborMap",
  fun () ->
    Jest.test (
      "builds neighbor map with bidirectional links",
      fun () ->
        let links = [
          { Source = "note1"; Target = "note2"; Value = 1.0 }
          { Source = "note2"; Target = "note3"; Value = 1.0 }
        ]

        let neighborMap = GraphView.buildNeighborMap links

        Jest.expect(neighborMap.ContainsKey "note1").toEqual (true)
        Jest.expect(neighborMap.ContainsKey "note2").toEqual (true)
        Jest.expect(neighborMap.ContainsKey "note3").toEqual (true)

        Jest.expect(neighborMap.["note1"].Contains "note2").toEqual (true)
        Jest.expect(neighborMap.["note2"].Contains "note1").toEqual (true)
        Jest.expect(neighborMap.["note2"].Contains "note3").toEqual (true)
        Jest.expect(neighborMap.["note3"].Contains "note2").toEqual (true)
    )

    Jest.test (
      "areNeighbors returns true for connected nodes",
      fun () ->
        let links = [ { Source = "note1"; Target = "note2"; Value = 1.0 } ]
        let neighborMap = GraphView.buildNeighborMap links

        Jest.expect(GraphView.areNeighbors neighborMap "note1" "note2").toEqual (true)
        Jest.expect(GraphView.areNeighbors neighborMap "note2" "note1").toEqual (true)
    )

    Jest.test (
      "areNeighbors returns false for unconnected nodes",
      fun () ->
        let links = [ { Source = "note1"; Target = "note2"; Value = 1.0 } ]
        let neighborMap = GraphView.buildNeighborMap links
        Jest.expect(GraphView.areNeighbors neighborMap "note1" "note3").toEqual (false)
    )

    Jest.test (
      "areNeighbors returns false for nonexistent nodes",
      fun () ->
        let neighborMap = GraphView.buildNeighborMap []
        Jest.expect(GraphView.areNeighbors neighborMap "note1" "note2").toEqual (false)
    )
)

Jest.describe (
  "GraphView.graphToGraphData",
  fun () ->
    Jest.test (
      "converts Graph to GraphData with correct degree calculation",
      fun () ->
        let graph = {
          Nodes = [ "note1"; "note2"; "note3" ]
          Edges = [
            { Source = "note1"; Target = "note2"; Type = "wiki" }
            { Source = "note2"; Target = "note3"; Type = "wiki" }
            { Source = "note1"; Target = "note3"; Type = "wiki" }
          ]
        }

        let graphData = GraphView.graphToGraphData graph

        Jest.expect(graphData.Nodes.Length).toEqual (3)
        Jest.expect(graphData.Links.Length).toEqual (3)

        let note1 = graphData.Nodes |> List.find (fun n -> n.Id = "note1")
        Jest.expect(note1.Degree).toEqual (2)

        let note2 = graphData.Nodes |> List.find (fun n -> n.Id = "note2")
        Jest.expect(note2.Degree).toEqual (2)

        let note3 = graphData.Nodes |> List.find (fun n -> n.Id = "note3")
        Jest.expect(note3.Degree).toEqual (2)
    )

    Jest.test (
      "handles nodes with no connections",
      fun () ->
        let graph = { Nodes = [ "note1"; "note2" ]; Edges = [] }
        let graphData = GraphView.graphToGraphData graph

        Jest.expect(graphData.Nodes.Length).toEqual (2)
        Jest.expect(graphData.Links.Length).toEqual (0)

        let note1 = graphData.Nodes |> List.find (fun n -> n.Id = "note1")
        Jest.expect(note1.Degree).toEqual (0)
    )
)
