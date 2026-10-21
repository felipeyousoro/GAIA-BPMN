import { useEffect, useRef } from "react";
import NavigatedViewer from "bpmn-js/lib/NavigatedViewer";
import type { Task } from "../api";

export function BpmnViewer({ xml, tasks, selectedId, onSelect }: { xml: string; tasks: Task[]; selectedId?: string; onSelect: (id: string) => void }) {
  const host = useRef<HTMLDivElement>(null);
  const viewer = useRef<NavigatedViewer>();
  useEffect(() => {
    if (!host.current) return;
    const instance = new NavigatedViewer({ container: host.current });
    viewer.current = instance;
    const select = (event: { element: { id: string; type: string } }) => {
      if (event.element.type.endsWith("Task") || event.element.type === "bpmn:CallActivity" || event.element.type === "bpmn:SubProcess") onSelect(event.element.id);
    };
    instance.on("element.click", select);
    void instance.importXML(xml).then(() => (instance.get("canvas") as { zoom: (mode: string) => void }).zoom("fit-viewport"));
    return () => { instance.destroy(); viewer.current = undefined; };
  }, [xml, onSelect]);
  useEffect(() => {
    const canvas = viewer.current?.get("canvas") as { addMarker: (id: string, marker: string) => void; removeMarker: (id: string, marker: string) => void } | undefined;
    if (!canvas) return;
    tasks.forEach(task => {
      const marker = task.input || task.output ? "has-values" : "no-values";
      canvas.addMarker(task.externalElementId, marker);
      if (selectedId === task.externalElementId) canvas.addMarker(task.externalElementId, "selected");
    });
    return () => tasks.forEach(task => { canvas.removeMarker(task.externalElementId, "has-values"); canvas.removeMarker(task.externalElementId, "no-values"); canvas.removeMarker(task.externalElementId, "selected"); });
  }, [tasks, selectedId]);
  return <div className="bpmn-canvas" ref={host} />;
}
