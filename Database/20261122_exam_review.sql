ALTER TABLE public.exams ADD COLUMN IF NOT EXISTS review_status text
    CHECK (review_status IS NULL OR (review_status IN ('pending', 'rejected') AND NOT is_published));
ALTER TABLE public.exams ALTER COLUMN is_published SET DEFAULT false;
