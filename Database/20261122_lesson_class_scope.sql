BEGIN;
LOCK TABLE public.lessons, public.class_lessons IN ACCESS EXCLUSIVE MODE;
ALTER TABLE public.class_lessons
    ADD COLUMN IF NOT EXISTS order_index integer,
    ALTER COLUMN schedule_date DROP NOT NULL,
    ALTER COLUMN schedule_id DROP NOT NULL,
    ALTER COLUMN teacher_id DROP NOT NULL;

-- Preserve every old class assignment, including lessons with no scheduled session.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'public' AND table_name = 'lessons' AND column_name = 'class_id') THEN
        INSERT INTO public.class_lessons (class_id, lesson_id, order_index, status, created_at)
        SELECT l.class_id, l.id, l.order_index, 'scheduled', CURRENT_TIMESTAMP
        FROM public.lessons l
        WHERE l.class_id IS NOT NULL AND NOT EXISTS
            (SELECT 1 FROM public.class_lessons cl WHERE cl.lesson_id = l.id AND cl.class_id = l.class_id);
        ALTER TABLE public.lessons DROP COLUMN class_id;
    END IF;
END $$;

-- Preserve class-specific ordering, even when one lesson has several scheduled sessions.
UPDATE public.class_lessons cl
SET order_index = l.order_index
FROM public.lessons l
WHERE cl.lesson_id = l.id AND cl.order_index IS NULL
  AND NOT EXISTS (SELECT 1 FROM public.class_lessons other
                  WHERE other.lesson_id = cl.lesson_id AND other.class_id = cl.class_id
                    AND other.order_index IS NOT NULL);

-- Independent classes may reuse the same order in a course.
DO $$
DECLARE item record;
BEGIN
  FOR item IN
    SELECT conname FROM pg_constraint
    WHERE conrelid = 'public.lessons'::regclass AND contype = 'u'
      AND (SELECT array_agg(a.attname::text ORDER BY a.attname)
           FROM unnest(conkey) k JOIN pg_attribute a ON a.attrelid=conrelid AND a.attnum=k)
          = ARRAY['course_id','order_index']::text[]
  LOOP EXECUTE format('ALTER TABLE public.lessons DROP CONSTRAINT %I', item.conname); END LOOP;
  FOR item IN
    SELECT idx.relname FROM pg_index i JOIN pg_class idx ON idx.oid=i.indexrelid
    WHERE i.indrelid='public.lessons'::regclass AND i.indisunique AND NOT i.indisprimary
      AND NOT EXISTS (SELECT 1 FROM pg_constraint c WHERE c.conindid=i.indexrelid)
      AND (SELECT array_agg(a.attname::text ORDER BY a.attname)
           FROM unnest(i.indkey::smallint[]) k JOIN pg_attribute a ON a.attrelid=i.indrelid AND a.attnum=k)
          = ARRAY['course_id','order_index']::text[]
  LOOP EXECUTE format('DROP INDEX public.%I', item.relname); END LOOP;
END $$;
CREATE INDEX IF NOT EXISTS ix_class_lessons_class_lesson ON public.class_lessons(class_id, lesson_id);
COMMIT;
